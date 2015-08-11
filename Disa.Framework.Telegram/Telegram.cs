using System;
using Disa.Framework.Bubbles;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpTelegram;
using SharpMTProto;
using SharpMTProto.Transport;
using SharpMTProto.Authentication;
using SharpTelegram.Schema.Layer18;
using System.Linq;
using SharpMTProto.Messaging.Handlers;
using SharpMTProto.Schema;
using System.Globalization;
using System.Timers;

//TODO:
//1) Incoming messages FullClient should be set to UnixNowTime, whereas downloaded messages should use the provided timestamp
//2) After authorization, there's an expiry time. Ensure that the login expires by then (also, in DC manager)
//3) _dialogs have to be refetched whenever there is a new conversation created, participant added, etc, to update the client's local cache.

namespace Disa.Framework.Telegram
{
    [ServiceInfo("Telegram", true, false, false, false, false, typeof(TelegramSettings), 
        ServiceInfo.ProcedureType.ConnectAuthenticate, typeof(TextBubble), typeof(PresenceBubble))]
    public partial class Telegram : Service, IVisualBubbleServiceId, ITerminal
    {
        private Dictionary<string, DisaThumbnail> _cachedThumbnails = new Dictionary<string, DisaThumbnail>();

        private static TcpClientTransportConfig DefaultTransportConfig = 
            new TcpClientTransportConfig("149.154.167.50", 443);

        private static TelegramAppInfo AppInfo = new TelegramAppInfo
        {
            ApiId = 19606,
            DeviceModel = "LG",
            SystemVersion = "5.0",
            AppVersion = "0.8.2",
            LangCode = PhoneBook.Language, //TODO: works?
        };

        private readonly object _baseMessageIdCounterLock = new object();
        private string _baseMessageId = "0000000000";
        private int _baseMessageIdCounter;

        public string CurrentMessageId
        {
            get
            {
                return _baseMessageId + Convert.ToString(_baseMessageIdCounter);
            }
        }

        public string NextMessageId
        {
            get
            {
                lock (_baseMessageIdCounterLock)
                {
                    _baseMessageIdCounter++;
                    return CurrentMessageId;
                }
            }
        }

        private bool _hasPresence;

        private Random _random = new Random(System.Guid.NewGuid().GetHashCode());

        private TelegramSettings _settings;
        private TelegramMutableSettings _mutableSettings;

        private TelegramClient _longPollClient;

        private TelegramClient _fullClientInternal;

        private readonly object _mutableSettingsLock = new object();

        private bool IsFullClientConnected
        {
            get
            {
                return _fullClientInternal != null && _fullClientInternal.IsConnected;
            }
        }

        //TODO: when the client dies when its not on a ping delay (user has Disa open),
        //      we may need to relaunch the client.
        private bool _fullClientWillDieBecauseOfPingDelay;

        private TelegramClient _fullClient
        {
            get
            {
                if (_fullClientInternal != null && _fullClientInternal.IsConnected)
                {
                    return _fullClientInternal;
                }
                Console.WriteLine(System.Environment.StackTrace);
                var transportConfig = 
                    new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                if (_fullClientInternal != null)
                {
                    _fullClientInternal.OnUpdateState -= OnUpdateState;
                    _fullClientInternal.OnUpdate -= OnUpdate;
                    _fullClientInternal.OnUpdateTooLong -= OnFullClientUpdateTooLong;
                }
                _fullClientInternal = new TelegramClient(transportConfig, 
                    new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo);
                _fullClientInternal.OnUpdateState += OnUpdateState;
                _fullClientInternal.OnUpdate += OnUpdate;
                _fullClientInternal.OnUpdateTooLong += OnFullClientUpdateTooLong;
                var result = TelegramUtils.RunSynchronously(_fullClientInternal.Connect());
                if (result != MTProtoConnectResult.Success)
                {
                    throw new Exception("Failed to connect: " + result);
                }
                SetFullClientPingDelayDisconnect();
                return _fullClientInternal;
            }
        }

        private MessagesDialogs _dialogs;
        private Config _config;

        private Dictionary<uint, Timer> _typingTimers = new Dictionary<uint, Timer>();

        private void CancelTypingTimer(uint userId)
        {
            if (_typingTimers.ContainsKey(userId))
            {
                var timer = _typingTimers[userId];
                timer.Stop();
                timer.Dispose();
            }
        }

        private void OnUpdateState(object sender, SharpMTProto.Messaging.Handlers.UpdatesHandler.State s)
        {
            Task.Factory.StartNew(() =>
            {
                SaveState(s.Date, s.Pts, s.Qts, s.Seq);
            });
        }

        private void SaveState(uint date, uint pts, uint qts, uint seq)
        {
            lock (_mutableSettingsLock)
            {
                DebugPrint("Saving new state");
                if (date != 0)
                {
                    _mutableSettings.Date = date;
                }
                if (pts != 0)
                {
                    _mutableSettings.Pts = pts;
                }
                if (qts != 0)
                {
                    _mutableSettings.Qts = qts;
                }
                if (seq != 0)
                {
                    _mutableSettings.Seq = seq;
                }
                MutableSettingsManager.Save(_mutableSettings);
            }
        }

        private object NormalizeUpdateIfNeeded(object obj)
        {
            // flatten UpdateNewMessage to Message
            var newMessage = obj as UpdateNewMessage;
            if (newMessage != null)
            {
                return newMessage.Message;
            }

            // convert ForwardedMessage to Message
            var forwardedMessage = obj as MessageForwarded;
            if (forwardedMessage != null)
            {
                return new SharpTelegram.Schema.Layer18.Message
                {
                    Flags = forwardedMessage.Flags,
                    Id = forwardedMessage.Id,
                    FromId = forwardedMessage.FromId,
                    ToId = forwardedMessage.ToId,
                    Date = forwardedMessage.Date,
                    MessageProperty = forwardedMessage.Message,
                    Media = forwardedMessage.Media,
                };
            }

            return obj;
        }

        private void OnUpdate(object sender, List<object> updates)
        {
            //NOTE: multiple client connects will call this event. Do not call upon _fullClient or any
            //      other connections in here.
            foreach (var updatez in updates)
            {
                var update = NormalizeUpdateIfNeeded(updatez);

                var shortMessage = update as UpdateShortMessage;
                var shortChatMessage = update as UpdateShortChatMessage;
                var typing = update as UpdateUserTyping;
                var userStatus = update as UpdateUserStatus;
                var readMessages = update as UpdateReadMessages;
                var message = update as SharpTelegram.Schema.Layer18.Message;

                if (shortMessage != null)
                {
                    var fromId = shortMessage.FromId.ToString(CultureInfo.InvariantCulture);
                    EventBubble(new TypingBubble(Time.GetNowUnixTimestamp(),
                        Bubble.BubbleDirection.Incoming,
                        fromId, false, this, false, false));
                    EventBubble(new TextBubble((long)shortMessage.Date, 
                        Bubble.BubbleDirection.Incoming, 
                        fromId, null, false, this, shortMessage.Message,
                        shortMessage.Id.ToString(CultureInfo.InvariantCulture)));
                    CancelTypingTimer(shortMessage.FromId);
                }
                else if (shortChatMessage != null)
                {
                    var address = shortChatMessage.ChatId.ToString(CultureInfo.InvariantCulture);
                    var participantAddress = shortChatMessage.FromId.ToString(CultureInfo.InvariantCulture);
                    EventBubble(new TextBubble((long)shortChatMessage.Date, 
                        Bubble.BubbleDirection.Incoming, 
                        address, participantAddress, true, this, shortChatMessage.Message,
                        shortChatMessage.Id.ToString(CultureInfo.InvariantCulture)));
                }
                else if (message != null)
                {
                    //TODO: media messages

                    TextBubble tb = null;

                    var user = message.ToId as PeerUser;
                    var chat = message.ToId as PeerChat;

                    var direction = message.FromId == _settings.AccountId 
                        ? Bubble.BubbleDirection.Outgoing : Bubble.BubbleDirection.Incoming;

                    if (user != null)
                    {
                        var address = direction == Bubble.BubbleDirection.Incoming ? message.FromId : user.UserId;
                        var addressStr = address.ToString(CultureInfo.InvariantCulture);
                        tb = new TextBubble((long)message.Date,
                                     direction, addressStr, null, false, this, message.MessageProperty,
                                     message.Id.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (chat != null)
                    {
                        var address = chat.ChatId.ToString(CultureInfo.InvariantCulture);
                        var participantAddress = message.FromId.ToString(CultureInfo.InvariantCulture);
                        tb = new TextBubble((long)message.Date,
                            direction, address, participantAddress, true, this, message.MessageProperty,
                            message.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    if (direction == Bubble.BubbleDirection.Outgoing)
                    {
                        tb.Status = Bubble.BubbleStatus.Sent;
                    }

                    EventBubble(tb);
                }
                else if (readMessages != null)
                {
                    //TODO:
                }
                else if (userStatus != null)
                {
                    var available = TelegramUtils.GetAvailable(userStatus.Status);
                    EventBubble(new PresenceBubble(Time.GetNowUnixTimestamp(),
                        Bubble.BubbleDirection.Incoming,
                        userStatus.UserId.ToString(CultureInfo.InvariantCulture),
                        false, this, available));
                }
                else if (typing != null)
                {
                    var isAudio = typing.Action is SendMessageRecordAudioAction;
                    var isTyping = typing.Action is SendMessageTypingAction;

                    if (isAudio || isTyping)
                    {
                        EventBubble(new TypingBubble(Time.GetNowUnixTimestamp(),
                            Bubble.BubbleDirection.Incoming,
                            typing.UserId.ToString(CultureInfo.InvariantCulture),
                            false, this, true, isAudio));
                        CancelTypingTimer(typing.UserId);
                        var newTimer = new Timer(6000) { AutoReset = false };
                        newTimer.Elapsed += (sender2, e2) =>
                        {
                            EventBubble(new TypingBubble(Time.GetNowUnixTimestamp(),
                                Bubble.BubbleDirection.Incoming,
                                typing.UserId.ToString(CultureInfo.InvariantCulture),
                                false, this, false, isAudio));
                            newTimer.Dispose();
                            _typingTimers.Remove(typing.UserId);
                        };
                        _typingTimers[typing.UserId] = newTimer;
                        newTimer.Start();
                    }

                    else
                    {
                        Console.WriteLine("Unknown typing action: " + typing.Action.GetType().Name);
                    }
                }
                else
                {
                    Console.WriteLine("Unknown update: " + ObjectDumper.Dump(update));
                }
            }
        }

        private void OnFullClientUpdateTooLong(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                FetchState(_fullClient);
            });
        }

        private void OnLongPollClientClosed(object sender, EventArgs e)
        {
            Utils.DebugPrint("Looks like a long poll client closed itself internally. Restarting Telegram...");
            RestartTelegram(null);
        }

        private void OnLongPollClientUpdateTooLong(object sender, EventArgs e)
        {
            if (IsFullClientConnected)
                return;
            Task.Factory.StartNew(() =>
            {
                var transportConfig = 
                    new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                using (var client = new TelegramClient(transportConfig, 
                    new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
                {
                    var result = TelegramUtils.RunSynchronously(client.Connect());
                    if (result != MTProtoConnectResult.Success)
                    {
                        throw new Exception("Failed to connect: " + result);
                    }  
                    FetchState(client);
                }
            });
        }

        private void Ping(TelegramClient client, Action<Exception> exception = null)
        {
            try
            {
                Utils.DebugPrint("Sending ping!");
                var pong = (Pong)TelegramUtils.RunSynchronously(client.ProtoMethods.PingAsync(new PingArgs
                {
                    PingId = GetRandomId(),
                }));
                Utils.DebugPrint("Got pong (from ping): " + pong.MsgId);
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to ping client: " + ex);
                if (exception != null)
                {
                    exception(ex);
                }
            }
        }
            
        private async void PingDelay(TelegramClient client, uint disconnectDelay, Action<Exception> exception = null)
        {
            try
            {
                Utils.DebugPrint("Sending pingDelay!");
                var pong = (Pong)await client.ProtoMethods.PingDelayDisconnectAsync(new PingDelayDisconnectArgs
                {
                    PingId = GetRandomId(),
                    DisconnectDelay = disconnectDelay
                });
                Utils.DebugPrint("Got pong (from pingDelay): " + pong.MsgId);
            }
            catch (Exception ex)
            {
                if (exception != null)
                {
                    exception(ex);
                }
            }
        }

        private void SetFullClientPingDelayDisconnect()
        {
            if (!IsFullClientConnected)
            {
                return;   
            }
            IPong iPong;
            if (_hasPresence)
            {
                DebugPrint("Telling full client that it can forever stay alive.");
                PingDelay(_fullClient, uint.MaxValue);
                _fullClientWillDieBecauseOfPingDelay = false;
                ScheduleFullClientPing();
            }
            else
            {
                DebugPrint("Telling full client that it can only stay alive for a minute.");
                PingDelay(_fullClient, 60);
                _fullClientWillDieBecauseOfPingDelay = true;
                RemoveFullClientPingIfPossible();
            }
        }

        private void RestartTelegram(Exception exception)
        {
            if (exception != null)
            {
                Utils.DebugPrint("Restarting Telegram: " + exception);
            }
            else
            {
                Utils.DebugPrint("Restarting Telegram");
            }
            // start a new task, freeing the possibility that there could be a wake lock being held
            Task.Factory.StartNew(() =>
            {
                ServiceManager.Restart(this);
            });
        }

        private WakeLockBalancer.GracefulWakeLock _longPollHeartbeart;
        private WakeLockBalancer.GracefulWakeLock _fullClientHeartbeat;

        private void ScheduleLongPollPing()
        {
            RemoveLongPollPingIfPossible();
            _longPollHeartbeart = new WakeLockBalancer.GracefulWakeLock(new WakeLockBalancer.ActionObject(() =>
            {
                if (_longPollClient == null || !_longPollClient.IsConnected)
                {
                    RemoveLongPollPingIfPossible();
                    RestartTelegram(null);
                }
                else
                {
                    Ping(_longPollClient, RestartTelegram);
                }
            }, WakeLockBalancer.ActionObject.ExecuteType.TaskWithWakeLock), 240, 60, true);
            Platform.ScheduleAction(_longPollHeartbeart);
        }

        private void RemoveLongPollPingIfPossible()
        {
            if (_longPollHeartbeart != null)
            {
                Platform.RemoveAction(_longPollHeartbeart);
                _longPollHeartbeart = null;
            }
        }

        private void ScheduleFullClientPing()
        {
            RemoveFullClientPingIfPossible();
            _fullClientHeartbeat = new WakeLockBalancer.GracefulWakeLock(new WakeLockBalancer.ActionObject(() =>
            {
                if (!IsFullClientConnected)
                {
                    RemoveFullClientPingIfPossible();   
                }
                else
                {
                    Ping(_fullClient);
                }
            }, WakeLockBalancer.ActionObject.ExecuteType.TaskWithWakeLock), 240, 60, true);
            Platform.ScheduleAction(_fullClientHeartbeat);
        }

        private void RemoveFullClientPingIfPossible()
        {
            if (_fullClientHeartbeat != null)
            {
                Platform.RemoveAction(_fullClientHeartbeat);
                _fullClientHeartbeat = null;
            }
        }

        private void DisconnectFullClientIfPossible()
        {
            if (IsFullClientConnected)
            {
                TelegramUtils.RunSynchronously(_fullClient.Methods.AccountUpdateStatusAsync(new AccountUpdateStatusArgs
                {
                    Offline = true
                }));
                try
                {
                    TelegramUtils.RunSynchronously(_fullClient.Disconnect());
                }
                catch (Exception ex)
                {
                    DebugPrint("Failed to disconnect full client: " + ex);
                }
                RemoveFullClientPingIfPossible();
            }
        }

        private void DisconnectLongPollerIfPossible()
        {
            if (_longPollClient != null && _longPollClient.IsConnected)
            {
                try
                {
                    TelegramUtils.RunSynchronously(_longPollClient.Disconnect());
                }
                catch (Exception ex)
                {
                    DebugPrint("Failed to disconnect full client: " + ex);
                }
                RemoveLongPollPingIfPossible();
            }
        }

        private ulong GetRandomId()
        {
            var buffer = new byte[sizeof(ulong)];
            _random.NextBytes(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public Telegram()
        {
            _baseMessageId = Convert.ToString(Time.GetNowUnixTimestamp());
        }

        public override bool Initialize(DisaSettings settings)
        {
            _settings = settings as TelegramSettings;
            _mutableSettings = MutableSettingsManager.Load<TelegramMutableSettings>();

            if (_settings.AuthKey == null)
            {
                return false;
            }

            return true;
        }

        public override bool InitializeDefault()
        {
            return false;
        }

        public static TelegramSettings GenerateAuthentication(Service service)
        {
            try
            {
                service.DebugPrint("Fetching nearest DC...");
                var settings = new TelegramSettings();
                var authInfo = TelegramUtils.RunSynchronously(FetchNewAuthentication(DefaultTransportConfig));
                using (var client = new TelegramClient(DefaultTransportConfig, 
                    new ConnectionConfig(authInfo.AuthKey, authInfo.Salt), AppInfo))
                {
                    TelegramUtils.RunSynchronously(client.Connect());
                    var nearestDcId = (NearestDc)TelegramUtils.RunSynchronously(client.Methods.HelpGetNearestDcAsync(new HelpGetNearestDcArgs{}));
                    var config = (Config)TelegramUtils.RunSynchronously(client.Methods.HelpGetConfigAsync(new HelpGetConfigArgs{ }));
                    var dcOption = config.DcOptions.OfType<DcOption>().FirstOrDefault(x => x.Id == nearestDcId.NearestDcProperty);
                    settings.NearestDcId = nearestDcId.NearestDcProperty;
                    settings.NearestDcIp = dcOption.IpAddress;
                    settings.NearestDcPort = (int)dcOption.Port;
                }
                service.DebugPrint("Generating authentication on nearest DC...");
                var authInfo2 = TelegramUtils.RunSynchronously(FetchNewAuthentication(
                                        new TcpClientTransportConfig(settings.NearestDcIp, settings.NearestDcPort)));
                settings.AuthKey = authInfo2.AuthKey;
                settings.Salt = authInfo2.Salt;
                service.DebugPrint("Great! Ready for the service to start.");
                return settings;
            }
            catch (Exception ex)
            {
                service.DebugPrint("Error in GenerateAuthentication: " + ex);
            }
            return null;
        }

        public class CodeRequest
        {
            public enum Type { Success, Failure, NumberInvalid, Migrate }

            public bool Registered { get; set; }
            public string CodeHash { get; set; }
            public Type Response { get; set; }
        }

        public static CodeRequest RequestCode(Service service, string number, string codeHash, TelegramSettings settings, bool call)
        {
            try
            {
                service.DebugPrint("Requesting code...");
                var transportConfig = 
                    new TcpClientTransportConfig(settings.NearestDcIp, settings.NearestDcPort);
                using (var client = new TelegramClient(transportConfig,
                    new ConnectionConfig(settings.AuthKey, settings.Salt), AppInfo))
                {
                    TelegramUtils.RunSynchronously(client.Connect());

                    if (!call)
                    {
                        try
                        {
                            var result = TelegramUtils.RunSynchronously(client.Methods.AuthSendCodeAsync(new AuthSendCodeArgs
                            {
                                PhoneNumber = number,
                                SmsType = 0,
                                ApiId = AppInfo.ApiId,
                                ApiHash = "f8f2562579817ddcec76a8aae4cd86f6",
                                LangCode = PhoneBook.Language
                            })) as AuthSentCode;
                            return new CodeRequest
                            {
                                Registered = result.PhoneRegistered,
                                CodeHash = result.PhoneCodeHash,
                            };
                        }
                        catch (RpcErrorException ex)
                        {
                            var error = (RpcError)ex.Error;
                            var cr = new CodeRequest();
                            var response = CodeRequest.Type.Failure;
                            switch (error.ErrorCode)
                            {
                                case 400:
                                    cr.Response = CodeRequest.Type.NumberInvalid;
                                    break;
                                default:
                                    cr.Response = CodeRequest.Type.Failure;
                                    break;
                            }
                            return cr;
                        }
                    }
                    var result2 = (bool)TelegramUtils.RunSynchronously(client.Methods.AuthSendCallAsync(new AuthSendCallArgs
                    {
                        PhoneNumber = number,
                        PhoneCodeHash = codeHash,
                    }));
                    return new CodeRequest
                    {
                        Response = result2 ? CodeRequest.Type.Success : CodeRequest.Type.Failure
                    };
                }
            }
            catch (Exception ex)
            {
                service.DebugPrint("Error in CodeRequest: " + ex);
            }
            return null;
        }

        public class CodeRegister
        {
            public enum Type { Success, Failure, NumberInvalid, CodeEmpty, CodeExpired, CodeInvalid, FirstNameInvalid, LastNameInvalid }

            public uint AccountId { get; set; }

            public long Expires { get; set; }
            public Type Response { get; set; }
        }

        public static CodeRegister RegisterCode(Service service, TelegramSettings settings, string number, string codeHash, string code, string firstName, string lastName, bool signIn)
        {
            try
            {
                service.DebugPrint("Registering code...");
                var transportConfig = 
                    new TcpClientTransportConfig(settings.NearestDcIp, settings.NearestDcPort);
                using (var client = new TelegramClient(transportConfig,
                    new ConnectionConfig(settings.AuthKey, settings.Salt), AppInfo))
                {
                    TelegramUtils.RunSynchronously(client.Connect());

                    try
                    {
                        IAuthAuthorization iresult = null;
                        if (signIn)
                        {
                            iresult = TelegramUtils.RunSynchronously(client.Methods.AuthSignInAsync(new AuthSignInArgs
                                {
                                    PhoneNumber = number,
                                    PhoneCodeHash = codeHash,
                                    PhoneCode = code,
                                }));
                        }
                        else
                        {
                            iresult = TelegramUtils.RunSynchronously(client.Methods.AuthSignUpAsync(new AuthSignUpArgs
                                {
                                    PhoneNumber = number,
                                    PhoneCodeHash = codeHash,
                                    PhoneCode = code,
                                    FirstName = firstName,
                                    LastName = lastName,
                                }));
                        }
                        var result = (AuthAuthorization)iresult;
                        return new CodeRegister
                        {
                            AccountId = (result.User as UserSelf).Id,
                            Expires = result.Expires,
                            Response = CodeRegister.Type.Success,
                        };
                    }
                    catch (RpcErrorException ex)
                    {
                        var error = (RpcError)ex.Error;
                        var cr = new CodeRegister();
                        var response = CodeRegister.Type.Failure;
                        switch (error.ErrorMessage)
                        {
                            case "PHONE_NUMBER_INVALID":
                                cr.Response = CodeRegister.Type.NumberInvalid;
                                break;
                            case "PHONE_CODE_EMPTY":
                                cr.Response = CodeRegister.Type.CodeEmpty;
                                break;
                            case "PHONE_CODE_EXPIRED":
                                cr.Response = CodeRegister.Type.CodeExpired;
                                break;
                            case "PHONE_CODE_INVALID":
                                cr.Response = CodeRegister.Type.CodeInvalid;
                                break;
                            case "FIRSTNAME_INVALID":
                                cr.Response = CodeRegister.Type.FirstNameInvalid;
                                break;
                            case "LASTNAME_INVALID":
                                cr.Response = CodeRegister.Type.LastNameInvalid;
                                break;
                            default:
                                cr.Response = CodeRegister.Type.Failure;
                                break;
                        }
                        return cr;
                    }
                }
            }
            catch (Exception ex)
            {
                service.DebugPrint("Error in CodeRequest: " + ex);
            }
            return null;
        }

        public async void DoCommand(string[] args)
        {
            var command = args[0].ToLower();

            switch (command)
            {
                case "setup":
                    {
                        DebugPrint("Fetching nearest DC...");
                        var telegramSettings = new TelegramSettings();
                        var authInfo = await FetchNewAuthentication(DefaultTransportConfig);
                        using (var client = new TelegramClient(DefaultTransportConfig, 
                            new ConnectionConfig(authInfo.AuthKey, authInfo.Salt), AppInfo))
                        {
                            await client.Connect();
                            var nearestDcId = (NearestDc)await(client.Methods.HelpGetNearestDcAsync(new HelpGetNearestDcArgs{}));
                            var config = (Config)await(client.Methods.HelpGetConfigAsync(new HelpGetConfigArgs{ }));
                            var dcOption = config.DcOptions.OfType<DcOption>().FirstOrDefault(x => x.Id == nearestDcId.NearestDcProperty);
                            telegramSettings.NearestDcId = nearestDcId.NearestDcProperty;
                            telegramSettings.NearestDcIp = dcOption.IpAddress;
                            telegramSettings.NearestDcPort = (int)dcOption.Port;
                        }
                        DebugPrint("Generating authentication on nearest DC...");
                        var authInfo2 = await FetchNewAuthentication(
                            new TcpClientTransportConfig(telegramSettings.NearestDcIp, telegramSettings.NearestDcPort));
                        telegramSettings.AuthKey = authInfo2.AuthKey;
                        telegramSettings.Salt = authInfo2.Salt;
                        SettingsManager.Save(this, telegramSettings);
                        DebugPrint("Great! Ready for the service to start.");
                    }
                    break;
                case "sendcode":
                    {
                        var number = args[1];
                        var transportConfig = 
                            new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                        using (var client = new TelegramClient(transportConfig, 
                                                new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
                        {
                            await client.Connect();
                            var result = await client.Methods.AuthSendCodeAsync(new AuthSendCodeArgs
                                {
                                    PhoneNumber = number,
                                    SmsType = 0,
                                    ApiId = AppInfo.ApiId,
                                    ApiHash = "f8f2562579817ddcec76a8aae4cd86f6",
                                    LangCode = PhoneBook.Language
                                });
                            DebugPrint(ObjectDumper.Dump(result));
                        }
                    }
                    break;
                case "signin":
                    {
                        var number = args[1];
                        var hash = args[2];
                        var code = args[3];
                        var transportConfig = 
                            new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                        using (var client = new TelegramClient(transportConfig, 
                                                new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
                        {
                            await client.Connect();
                            var result = (AuthAuthorization)await client.Methods.AuthSignInAsync(new AuthSignInArgs
                            {
                                PhoneNumber = number,
                                PhoneCodeHash = hash,
                                PhoneCode = code,
                            });
                            DebugPrint(ObjectDumper.Dump(result));
                        }
                    }
                    break;
                case "signup":
                    {
                        var number = args[1];
                        var hash = args[2];
                        var code = args[3];
                        var firstName = args[4];
                        var lastName = args[5];
                        var transportConfig = 
                            new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                        using (var client = new TelegramClient(transportConfig, 
                            new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
                        {
                            await client.Connect();
                            var result = (AuthAuthorization)await client.Methods.AuthSignUpAsync(new AuthSignUpArgs
                            {
                                PhoneNumber = number,
                                PhoneCodeHash = hash,
                                PhoneCode = code,
                                FirstName = firstName,
                                LastName = lastName,
                            });
                            DebugPrint(ObjectDumper.Dump(result));
                        }
                    }
                    break;
                case "getcontacts":
                    {
                        var result = await _fullClient.Methods.ContactsGetContactsAsync(new ContactsGetContactsArgs
                        {
                            Hash = string.Empty
                        });
                        DebugPrint(ObjectDumper.Dump(result));
                    }
                    break;
                case "sendhello":
                    {
                        var contacts = (ContactsContacts)await _fullClient.Methods.ContactsGetContactsAsync(new ContactsGetContactsArgs
                            {
                                Hash = string.Empty
                            });
                        var counter = 0;
                        Console.WriteLine("Pick a contact:");
                        foreach (var icontact in contacts.Users)
                        {
                            var contact = icontact as UserContact;
                            if (contact == null)
                                continue;
                            Console.WriteLine(counter++ + ") " + contact.FirstName + " " + contact.LastName);
                        }
                        var choice = int.Parse(Console.ReadLine());
                        var chosenContact = (UserContact)contacts.Users[choice];
                        var result = await _fullClient.Methods.MessagesSendMessageAsync(new MessagesSendMessageArgs
                            {
                                Peer = new InputPeerContact
                                    {
                                        UserId = chosenContact.Id,
                                    },
                                Message = "Hello from Disa!",
                                RandomId = (ulong)Time.GetNowUnixTimestamp(),
                            });
                        Console.WriteLine(ObjectDumper.Dump(result));
                    }
                    break;
            }

        }

        private static async Task<AuthInfo> FetchNewAuthentication(TcpClientTransportConfig config)
        {
            var authKeyNegotiater = MTProtoClientBuilder.Default.BuildAuthKeyNegotiator(config);
            authKeyNegotiater.KeyChain.Add(RSAPublicKey.Get());

            return await authKeyNegotiater.CreateAuthKey();
        }

        private uint GetNearestDc()
        {
            var nearestDc = (NearestDc)TelegramUtils.RunSynchronously(
                _fullClient.Methods.HelpGetNearestDcAsync(new HelpGetNearestDcArgs{}));
            return nearestDc.NearestDcProperty;
        }

        private Tuple<string, uint> GetDcIPAndPort(uint id)
        {
            var config = (Config)TelegramUtils.RunSynchronously(_fullClient.Methods.HelpGetConfigAsync(new HelpGetConfigArgs{ }));
            var dcOption = config.DcOptions.OfType<DcOption>().FirstOrDefault(x => x.Id == id);
            return Tuple.Create(dcOption.IpAddress, dcOption.Port);
        }

//        public void ChangeDcIfNeeded()
//        {
//            var nearestDc = GetNearestDc();
//            if (_mutableSettings.NearestDcId != nearestDc) // what if the first DC is zero? fix this case
//            {
//                var ipAndPort = GetDcIPAndPort(nearestDc);
//                _mutableSettings.NearestDcIp = ipAndPort.Item1;
//                _mutableSettings.NearestDcPort = ipAndPort.Item2;
//
//                MutableSettingsManager.Save(_mutableSettings);
//                throw new ServiceSpecialRestartException("Changing DCs");
//            }
//        }

        public override bool Authenticate(WakeLock wakeLock)
        {
            return true;
        }

        public override void Deauthenticate()
        {
            // do nothing
        }

        private void FetchDifference(TelegramClient client)
        {
            var counter = 0;

            DebugPrint("Fetching difference");

            Again:

            DebugPrint("Difference Page: " + counter);

            var difference = TelegramUtils.RunSynchronously(
                client.Methods.UpdatesGetDifferenceAsync(new UpdatesGetDifferenceArgs
            {
                Date = _mutableSettings.Date,
                Pts = _mutableSettings.Pts,
                Qts = _mutableSettings.Qts
            }));

            var empty = difference as UpdatesDifferenceEmpty;
            var diff = difference as UpdatesDifference;
            var slice = difference as UpdatesDifferenceSlice;

            Action dispatchUpdates = () =>
            {
                var updates = new List<object>();
                //TODO: encrypyed messages
                if (diff != null)
                {
                    updates.AddRange(diff.NewMessages);
                    updates.AddRange(diff.OtherUpdates);
                }
                else
                {
                    updates.AddRange(slice.NewMessages);
                    updates.AddRange(slice.OtherUpdates);
                }
                DebugPrint(ObjectDumper.Dump(updates));
                OnUpdate(null, updates);
            };

            if (diff != null)
            {
                dispatchUpdates();
                var state = (UpdatesState)diff.State;
                SaveState(state.Date, state.Pts, state.Qts, state.Seq);
            }
            else if (slice != null)
            {
                dispatchUpdates();
                var state = (UpdatesState)slice.IntermediateState;
                SaveState(state.Date, state.Pts, state.Qts, state.Seq);
                counter++;
                goto Again;
            }
            else if (empty != null)
            {
                SaveState(empty.Date, 0, 0, empty.Seq);
            }
        }


        private void FetchState(TelegramClient client)
        {
            if (_mutableSettings.Date == 0)
            {
                DebugPrint("We need to fetch the state!");
                var state = (UpdatesState)TelegramUtils.RunSynchronously(client.Methods.UpdatesGetStateAsync(new UpdatesGetStateArgs()));
                SaveState(state.Date, state.Pts, state.Qts, state.Seq);
            }
            else
            {
                FetchDifference(client);
            }
        }

        public override void Connect(WakeLock wakeLock)
        {
            var sessionId = GetRandomId();
            var transportConfig = 
                new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
            using (var client = new TelegramClient(transportConfig, 
                                    new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
            {
                var result = TelegramUtils.RunSynchronously(client.Connect());
                if (result != MTProtoConnectResult.Success)
                {
                    throw new Exception("Failed to connect: " + result);
                }   
                DebugPrint("Registering long poller...");
                var registerDeviceResult = TelegramUtils.RunSynchronously(client.Methods.AccountRegisterDeviceAsync(
                    new AccountRegisterDeviceArgs
                {
                    TokenType = 7,
                    Token = sessionId.ToString(CultureInfo.InvariantCulture),
                    DeviceModel = AppInfo.DeviceModel,
                    SystemVersion = AppInfo.SystemVersion,
                    AppVersion = AppInfo.AppVersion,
                    AppSandbox = false,
                    LangCode = AppInfo.LangCode
                }));
                if (!registerDeviceResult)
                {
                    throw new Exception("Failed to register long poller...");
                }
                FetchState(client);
                GetDialogs(client);
                GetConfig(client);
            }
            DebugPrint("Starting long poller...");
            if (_longPollClient != null)
            {
                _longPollClient.OnUpdateTooLong -= OnLongPollClientUpdateTooLong;
            }
            _longPollClient = new TelegramClient(transportConfig, 
                new ConnectionConfig(_settings.AuthKey, _settings.Salt) { SessionId = sessionId }, AppInfo);
            var result2 = TelegramUtils.RunSynchronously(_longPollClient.Connect());
            if (result2 != MTProtoConnectResult.Success)
            {
                throw new Exception("Failed to connect long poll client: " + result2);
            } 
            _longPollClient.OnUpdateTooLong += OnLongPollClientUpdateTooLong;
            _longPollClient.OnClosedInternally += OnLongPollClientClosed;
            ScheduleLongPollPing();
            DebugPrint("Long poller started!");
        }

        public override void Disconnect()
        {
            DisconnectFullClientIfPossible();
            DisconnectLongPollerIfPossible();
        }

        public override string GetIcon(bool large)
        {
            if (large)
            {
                return Constants.LargeIcon;
            }

            return Constants.SmallIcon;
        }

        public override IEnumerable<Bubble> ProcessBubbles()
        {
            throw new NotImplementedException();
        }

        public override async void SendBubble(Bubble b)
        {
            var presenceBubble = b as PresenceBubble;
            if (presenceBubble != null)
            {
                _hasPresence = presenceBubble.Available;
                SetFullClientPingDelayDisconnect();
                if (_hasPresence)
                {
                    var updatedUsers = GetUpdatedUsersOfAllDialogs();
                    if (updatedUsers != null)
                    {
                        foreach (var updatedUser in updatedUsers)
                        {
                            EventBubble(new PresenceBubble(Time.GetNowUnixTimestamp(), Bubble.BubbleDirection.Incoming, 
                                TelegramUtils.GetUserId(updatedUser), false, this, TelegramUtils.GetAvailable(updatedUser)));
                        }
                    }
                }
                await _fullClient.Methods.AccountUpdateStatusAsync(new AccountUpdateStatusArgs
                    {
                        Offline = !presenceBubble.Available
                    });
            }


            var textBubble = b as TextBubble;
            if (textBubble != null)
            {
                var peer = GetInputPeer(textBubble.Address, textBubble.Party);
                TelegramUtils.RunSynchronously(_fullClient.Methods.MessagesSendMessageAsync(new MessagesSendMessageArgs
                    {
                        Peer = peer,
                        Message = textBubble.Message,
                        RandomId = ulong.Parse(textBubble.IdService)
                    }));
            }
        }

        private IInputPeer GetInputPeer(string userId, bool groupChat)
        {
            if (groupChat)
            {
                return new InputPeerChat
                {
                    ChatId = uint.Parse(userId)
                };
            }
            else
            {
                var accessHash = GetUserAccessHashIfForeign(userId);
                if (accessHash != 0)
                {
                    return new InputPeerForeign
                    {
                        UserId = uint.Parse(userId),
                        AccessHash = accessHash
                    };
                }
                else
                {
                    return new InputPeerContact
                    {
                        UserId = uint.Parse(userId)
                    };
                }
            }
        }

        private ulong GetUserAccessHashIfForeign(string userId)
        {
            if (_dialogs == null)
                return 0;
            foreach (var user in _dialogs.Users)
            {
                var userForeign = user as UserForeign;
                if (userForeign != null)
                {
                    var userForeignId = TelegramUtils.GetUserId(userForeign);
                    if (userForeignId == userId)
                        return TelegramUtils.GetAccessHash(userForeign);
                }
            }
            return 0;
        }

        public override bool BubbleGroupComparer(string first, string second)
        {
            return first == second;
        }

        public override Task GetBubbleGroupLegibleId(BubbleGroup group, Action<string> result)
        {
            throw new NotImplementedException();
        }

        private async Task<List<IUser>> GetUsers(List<IInputUser> users, TelegramClient client)
        {
            var response = await client.Methods.UsersGetUsersAsync(new UsersGetUsersArgs
            {
                Id = users
            });
            return response;
        }

        private async Task<IUser> GetUser(IInputUser user, TelegramClient client)
        {
            return (await GetUsers(new List<IInputUser> { user }, client)).First();
        }

        public override Task GetBubbleGroupName(BubbleGroup group, Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(GetTitle(group.Address, group.IsParty));
            });
        }

        public override Task GetBubbleGroupPhoto(BubbleGroup group, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(GetThumbnail(group.Address, group.IsParty, true));
            });
        }


        public override Task GetBubbleGroupPartyParticipants(BubbleGroup group, Action<DisaParticipant[]> result)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var clientDisposable = new TelegramClientDisposable(this))
                {
                    var fullChat = (MessagesChatFull)TelegramUtils.RunSynchronously(clientDisposable.Client.Methods.MessagesGetFullChatAsync(new MessagesGetFullChatArgs
                    {
                        ChatId = uint.Parse(group.Address) 
                    }));
                    var chatFull = (ChatFull)fullChat.FullChat;
                    var participants = chatFull.Participants as ChatParticipants;
                    if (participants == null)
                    {
                        result(null);
                    }
                    else
                    {
                        var users = participants.Participants.Select(x => 
                            GetUser(fullChat.Users, ((ChatParticipant)x).UserId.ToString(CultureInfo.InvariantCulture)));
                        var disaParticipants = users.Select(x => 
                            new DisaParticipant(
                                TelegramUtils.GetUserName(x), 
                                TelegramUtils.GetUserId(x))).ToArray();
                        result(disaParticipants);
                    }
                }
            });
        }

        public override Task GetBubbleGroupUnknownPartyParticipant(BubbleGroup group, string unknownPartyParticipant, Action<DisaParticipant> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(null);
            });
        }

        public override Task GetBubbleGroupPartyParticipantPhoto(DisaParticipant participant, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                //TODO: results from partyparticipants need to be cached
                // GetThumbnails should then search those as well,
                // because _dialog.Users does not contain all users in participant list
                result(GetThumbnail(participant.Address, false, true));
            });
        }

        public override Task GetBubbleGroupLastOnline(BubbleGroup group, Action<long> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(GetUpdatedLastOnline(group.Address));
            });
        }

        public void AddVisualBubbleIdServices(VisualBubble bubble)
        {
            bubble.IdService = NextMessageId;
        }

        public bool DisctinctIncomingVisualBubbleIdServices()
        {
            return true;
        }

        public override void RefreshPhoneBookContacts()
        {
//            Client.Methods.ContactsImportContactsAsync(new ContactsImportContactsArgs
//                {

//                    Contacts = new List<IInputContact>
//                        {
//                            
//                        };
//                    Replace = false,
//                });
            base.RefreshPhoneBookContacts();
        }

        private IUser GetUser(List<IUser> users, string userId)
        {
            if (_dialogs == null)
                return null;
            foreach (var user in users)
            {
                var userIdInner = TelegramUtils.GetUserId(user);
                if (userId == userIdInner)
                {
                    return user;
                }
            }
            return null;
        }

        private List<IUser> GetUpdatedUsersOfAllDialogs()
        {
            if (_dialogs == null)
                return null;
            var peerUsers = _dialogs.Dialogs.Select(x => (x as Dialog).Peer).OfType<PeerUser>();
            var users = new List<IUser>();
            foreach (var peer in peerUsers)
            {
                var peerUserId = peer.UserId.ToString(CultureInfo.InvariantCulture);
                foreach (var user in _dialogs.Users)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (peerUserId == userId)
                    {
                        users.Add(user);
                    }
                }
            }
            using (var clientDisposable = new TelegramClientDisposable(this))
            {
                var inputUsers = users.Select(x => TelegramUtils.CastUserToInputUser(x)).Where(d => d != null).ToList();
                var updatedUsers = TelegramUtils.RunSynchronously(GetUsers(inputUsers, clientDisposable.Client));
                return updatedUsers;
            }
        }

        private long GetUpdatedLastOnline(string id)
        {
            if (_dialogs == null)
                return 0;
            foreach (var user in _dialogs.Users)
            {
                var userId = TelegramUtils.GetUserId(user);
                if (userId == id)
                {
                    var inputUser = TelegramUtils.CastUserToInputUser(user);
                    if (inputUser != null)
                    {
                        using (var clientDisposable = new TelegramClientDisposable(this))
                        {
                            var updatedUser = TelegramUtils.RunSynchronously(GetUser(inputUser, clientDisposable.Client));
                            return TelegramUtils.GetLastSeenTime(updatedUser);
                        }
                    }
                }
            }
            DebugPrint("Could not get last online for user: " + id);
            return 0;
        }

        private string GetTitle(string id, bool group)
        {
            if (_dialogs == null)
                return null;
            if (group)
            {
                foreach (var chat in _dialogs.Chats)
                {
                    var chatId = TelegramUtils.GetChatId(chat);
                    if (chatId == id)
                    {
                        return TelegramUtils.GetChatTitle(chat);
                    }
                }
            }
            else
            {
                foreach (var user in _dialogs.Users)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (userId == id)
                    {
                        return TelegramUtils.GetUserName(user);
                    }
                }
            }
            DebugPrint("Could not get title for user: " + id);
            return null;
        }

        private DisaThumbnail GetThumbnail(string id, bool group, bool small)
        {
            Func<DisaThumbnail, DisaThumbnail> rturn = thumbnail =>
            {
                lock (_cachedThumbnails)
                {
                    _cachedThumbnails[id] = thumbnail;
                }
                return thumbnail;
            };

            lock (_cachedThumbnails)
            {
                if (_cachedThumbnails.ContainsKey(id))
                {
                    return _cachedThumbnails[id];
                }
            }
            if (_dialogs == null)
                return null;
            if (group)
            {
                foreach (var chat in _dialogs.Chats)
                {
                    if (id == TelegramUtils.GetChatId(chat))
                    {
                        var fileLocation = TelegramUtils.GetChatThumbnailLocation(chat, small);
                        if (fileLocation == null)
                        {
                            return rturn(null);
                        }
                        else
                        {
                            var bytes = FetchFileBytes(fileLocation);
                            return rturn(new DisaThumbnail(this, bytes, id));
                        }
                    }
                }
            }
            else
            {
                foreach (var user in _dialogs.Users)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (userId == id)
                    {
                        var fileLocation = TelegramUtils.GetUserPhotoLocation(user, small);
                        if (fileLocation == null)
                        {
                            return rturn(null);
                        }
                        else
                        {
                            var bytes = FetchFileBytes(fileLocation);
                            return rturn(new DisaThumbnail(this, bytes, id));
                        }
                    }
                }
            }
            return null;
        }

        //TODO: chunk the download
        private static byte[] FetchFileBytes(TelegramClient client, FileLocation fileLocation)
        {
            var response = (UploadFile)TelegramUtils.RunSynchronously(client.Methods.UploadGetFileAsync(
                new UploadGetFileArgs
            {
                Location = new InputFileLocation
                {
                    VolumeId = fileLocation.VolumeId,
                    LocalId = fileLocation.LocalId,
                    Secret = fileLocation.Secret
                },
                Offset = 0,
                Limit = uint.MaxValue,
            }));
            return response.Bytes;
        }
            
        private byte[] FetchFileBytes(FileLocation fileLocation)
        {
            if (fileLocation.DcId == _settings.NearestDcId)
            {
                using (var clientDisposable = new TelegramClientDisposable(this))
                {
                    return FetchFileBytes(clientDisposable.Client, fileLocation);
                }   
            }
            else
            {
                try
                {
                    var client = GetClient((int)fileLocation.DcId);
                    return FetchFileBytes(client, fileLocation);
                }
                catch (Exception ex)
                {
                    DebugPrint("Failed to obtain client from DC manager: " + ex);
                    return null;
                }
            }
        }

        private void GetConfig(TelegramClient client)
        {
            var config = (Config)TelegramUtils.RunSynchronously(client.Methods.HelpGetConfigAsync(new HelpGetConfigArgs
                {
                }));
            _config = config;
        }

        private void GetDialogs(TelegramClient client)
        {
            DebugPrint("Fetching conversations");
            _dialogs = new MessagesDialogs
            {
                Chats = new List<IChat>(),
                Dialogs = new List<IDialog>(),
                Messages = new List<SharpTelegram.Schema.Layer18.IMessage>(),
                Users = new List<IUser>(),
            };
            uint limit = 10;
            uint offset = 0;
            Again:
            var iDialogs = TelegramUtils.RunSynchronously(client.Methods.MessagesGetDialogsAsync(new MessagesGetDialogsArgs
                {
                    Limit = limit,
                    Offset = offset,
                    MaxId = 0,
                }));
            var dialogs = iDialogs as MessagesDialogs;
            var dialogsSlice = iDialogs as MessagesDialogsSlice;
            if (dialogs != null)
            {
                _dialogs.Chats.AddRange(dialogs.Chats);
                _dialogs.Dialogs.AddRange(dialogs.Dialogs);
                _dialogs.Messages.AddRange(dialogs.Messages);
                _dialogs.Users.AddRange(dialogs.Users);
            }
            else if (dialogsSlice != null)
            {
                _dialogs.Chats.AddRange(dialogsSlice.Chats);
                _dialogs.Dialogs.AddRange(dialogsSlice.Dialogs);
                _dialogs.Messages.AddRange(dialogsSlice.Messages);
                _dialogs.Users.AddRange(dialogsSlice.Users);
                if (dialogsSlice.Count < offset + limit)
                {
                    Console.WriteLine("No need to fetch anymore slices. We've reached the end!");
                    goto End;
                }
                DebugPrint("Obtained a dialog slice! ... fetching more!");
                offset += limit;
                goto Again;
            }
            End:
            DebugPrint("Obtained conversations.");
        }

        private class TelegramClientDisposable : IDisposable
        {
            private readonly bool _isFullClient;
            private readonly TelegramClient _client;

            public TelegramClient Client
            {
                get
                {
                    return _client;
                }
            }

            public TelegramClientDisposable(Telegram telegram)
            {
                if (telegram.IsFullClientConnected)
                {
                    _client = telegram._fullClient;
                    _isFullClient = true;
                }
                else
                {
                    var transportConfig = 
                        new TcpClientTransportConfig(telegram._settings.NearestDcIp, telegram._settings.NearestDcPort);
                    var client = new TelegramClient(transportConfig, 
                        new ConnectionConfig(telegram._settings.AuthKey, telegram._settings.Salt), AppInfo);
                    var result = TelegramUtils.RunSynchronously(client.Connect());
                    if (result != MTProtoConnectResult.Success)
                    {
                        throw new Exception("Failed to connect: " + result);
                    }
                    _client = client;
                    _isFullClient = false;
                }
            }

            public void Dispose()
            {
                if (!_isFullClient)
                {
                    _client.Dispose();
                }
            }
        }
    }

    public class TelegramSettings : DisaSettings
    {
        public byte[] AuthKey { get; set; }
        public ulong Salt { get; set; }
        public uint NearestDcId { get; set; }
        public string NearestDcIp { get; set; }
        public int NearestDcPort { get; set; }
        public uint AccountId { get; set; }
    }

    public class TelegramMutableSettings : DisaMutableSettings
    {
        public uint Date { get; set; }
        public uint Pts { get; set; }
        public uint Qts { get; set; }
        public uint Seq { get; set; }
    }
}

