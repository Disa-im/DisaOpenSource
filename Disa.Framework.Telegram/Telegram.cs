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
//1) After authorization, there's an expiry time. Ensure that the login expires by then (also, in DC manager)

namespace Disa.Framework.Telegram
{
    [ServiceInfo("Telegram", true, false, false, false, false, typeof(TelegramSettings), 
        ServiceInfo.ProcedureType.ConnectAuthenticate, typeof(TextBubble), typeof(TypingBubble), typeof(PresenceBubble))]
    public partial class Telegram : Service, IVisualBubbleServiceId, ITerminal
    {
        private Dictionary<string, DisaThumbnail> _cachedThumbnails = new Dictionary<string, DisaThumbnail>();

        private static TcpClientTransportConfig DefaultTransportConfig = 
            new TcpClientTransportConfig("149.154.167.50", 443);

        private readonly object _baseMessageIdCounterLock = new object();
        private string _baseMessageId = "0000000000";
        private int _baseMessageIdCounter;

        public bool LoadConversations;

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

        private bool _longPollerAborted;

        private Random _random = new Random(System.Guid.NewGuid().GetHashCode());

        private TelegramSettings _settings;
        private TelegramMutableSettings _mutableSettings;

        private TelegramClient _longPollClient;

        private readonly object _mutableSettingsLock = new object();

        private CachedDialogs _dialogs = new CachedDialogs();
        private bool _dialogsInitiallyRetrieved = false;
        private Config _config;

        private Dictionary<uint, Timer> _typingTimers = new Dictionary<uint, Timer>();

        private WakeLockBalancer.GracefulWakeLock _longPollHeartbeart;

        private void CancelTypingTimer(uint userId)
        {
            if (_typingTimers.ContainsKey(userId))
            {
                var timer = _typingTimers[userId];
                timer.Stop();
                timer.Dispose();
            }
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

        private void ProcessIncomingPayload(List<object> payloads, bool useCurrentTime, TelegramClient optionalClient = null)
        {
            foreach (var payload in payloads)
            {
                var update = NormalizeUpdateIfNeeded(payload);

                var shortMessage = update as UpdateShortMessage;
                var shortChatMessage = update as UpdateShortChatMessage;
                var typing = update as UpdateUserTyping;
                var userStatus = update as UpdateUserStatus;
                var readMessages = update as UpdateReadMessages;
                var messageService = update as MessageService;
                var updateChatParticipants = update as UpdateChatParticipants;
                var message = update as SharpTelegram.Schema.Layer18.Message;
                var user = update as IUser;
                var chat = update as IChat;

                if (shortMessage != null)
                {
                    if (!string.IsNullOrWhiteSpace(shortMessage.Message))
                    {
                        var fromId = shortMessage.FromId.ToString(CultureInfo.InvariantCulture);
                        EventBubble(new TypingBubble(Time.GetNowUnixTimestamp(),
                            Bubble.BubbleDirection.Incoming,
                            fromId, false, this, false, false));
                        EventBubble(new TextBubble(
                            useCurrentTime ? Time.GetNowUnixTimestamp() : (long)shortMessage.Date, 
                            Bubble.BubbleDirection.Incoming, 
                            fromId, null, false, this, shortMessage.Message,
                            shortMessage.Id.ToString(CultureInfo.InvariantCulture)));
                        CancelTypingTimer(shortMessage.FromId);
                    }
                }
                else if (shortChatMessage != null)
                {
                    if (!string.IsNullOrWhiteSpace(shortChatMessage.Message))
                    {
                        var address = shortChatMessage.ChatId.ToString(CultureInfo.InvariantCulture);
                        var participantAddress = shortChatMessage.FromId.ToString(CultureInfo.InvariantCulture);
                        EventBubble(new TextBubble(
                            useCurrentTime ? Time.GetNowUnixTimestamp() : (long)shortChatMessage.Date, 
                            Bubble.BubbleDirection.Incoming, 
                            address, participantAddress, true, this, shortChatMessage.Message,
                            shortChatMessage.Id.ToString(CultureInfo.InvariantCulture)));
                    }
                }
                else if (message != null)
                {
                    if (!string.IsNullOrWhiteSpace(message.MessageProperty))
                    {
                        TextBubble tb = null;

                        var peerUser = message.ToId as PeerUser;
                        var peerChat = message.ToId as PeerChat;

                        var direction = message.FromId == _settings.AccountId 
                        ? Bubble.BubbleDirection.Outgoing : Bubble.BubbleDirection.Incoming;

                        if (peerUser != null)
                        {
                            var address = direction == Bubble.BubbleDirection.Incoming ? message.FromId : peerUser.UserId;
                            var addressStr = address.ToString(CultureInfo.InvariantCulture);
                            tb = new TextBubble(
                                useCurrentTime ? Time.GetNowUnixTimestamp() : (long)message.Date,
                                direction, addressStr, null, false, this, message.MessageProperty,
                                message.Id.ToString(CultureInfo.InvariantCulture));
                        }
                        else if (peerChat != null)
                        {
                            var address = peerChat.ChatId.ToString(CultureInfo.InvariantCulture);
                            var participantAddress = message.FromId.ToString(CultureInfo.InvariantCulture);
                            tb = new TextBubble(
                                useCurrentTime ? Time.GetNowUnixTimestamp() : (long)message.Date,
                                direction, address, participantAddress, true, this, message.MessageProperty,
                                message.Id.ToString(CultureInfo.InvariantCulture));
                        }

                        if (direction == Bubble.BubbleDirection.Outgoing)
                        {
                            tb.Status = Bubble.BubbleStatus.Sent;
                        }

                        EventBubble(tb);
                    }
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
                else if (user != null)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (userId != null)
                    {
                        var updatedUser = false;
                        for (int i = 0; i < _dialogs.Users.Count; i++)
                        {
                            var userInnerId = TelegramUtils.GetUserId(_dialogs.Users[i]);
                            if (userInnerId != null && userInnerId == userId)
                            {
                                Console.WriteLine("Updating user with new updates information: " + userId);
                                _dialogs.Users[i] = user;
                                updatedUser = true;
                                break;
                            }
                        }
                        if (!updatedUser)
                        {
                            Console.WriteLine("New user information: " + userId + " adding to dialogs!");
                            _dialogs.Users.Add(user);
                        }
                    }
                }
                else if (chat != null)
                {
                    var chatId = TelegramUtils.GetChatId(chat);
                    if (chatId != null)
                    {
                        var updatedChat = false;
                        for (int i = 0; i < _dialogs.Chats.Count; i++)
                        {
                            var chatInnerId = TelegramUtils.GetChatId(_dialogs.Chats[i]);
                            if (chatInnerId != null && chatInnerId == chatId)
                            {
                                Console.WriteLine("Updating chat with new updates information: " + chatId);
                                _dialogs.Chats[i] = chat;
                                updatedChat = true;
                                break;
                            }
                        }
                        if (!updatedChat)
                        {
                            Console.WriteLine("New chat information: " + chatId + " adding to dialogs!");
                            _dialogs.Chats.Add(chat);
                        }
                    }
                }
                else if (updateChatParticipants != null)
                {
                    var chatParicipants = updateChatParticipants.Participants as ChatParticipants;
                    if (chatParicipants != null)
                    {
                        var address = chatParicipants.ChatId.ToString(CultureInfo.InvariantCulture);
                        DebugPrint("Updating chat participants: " + address);
                        RemoveFullChat(address);
                        GetFullChat(address);
                        BubbleGroupUpdater.Update(this, address);
                    }
                }
                else if (messageService != null)
                {
                    var editTitle = messageService.Action as MessageActionChatEditTitle;
                    var deleteUser = messageService.Action as MessageActionChatDeleteUser;
                    var addUser = messageService.Action as MessageActionChatAddUser;
                    var created = messageService.Action as MessageActionChatCreate;

                    var address = TelegramUtils.GetPeerId(messageService.ToId);
                    var fromId = messageService.FromId.ToString(CultureInfo.InvariantCulture);
                    if (editTitle != null)
                    {
                        var newTitle = editTitle.Title;
                        for (int i = 0; i < _dialogs.Chats.Count; i++)
                        {
                            var chatInnerId = TelegramUtils.GetChatId(_dialogs.Chats[i]);
                            if (chatInnerId != null && chatInnerId == address)
                            {
                                TelegramUtils.SetChatTitle(_dialogs.Chats[i], newTitle);
                                break;
                            }
                        }
                        EventBubble(PartyInformationBubble.CreateTitleChanged(Time.GetNowUnixTimestamp(), address, 
                            this, messageService.Id.ToString(CultureInfo.InvariantCulture), fromId, newTitle));
                        BubbleGroupUpdater.Update(this, address);
                    }
                    else if (deleteUser != null)
                    {
                        var userDeleted = deleteUser.UserId.ToString(CultureInfo.InvariantCulture);
                        EventBubble(PartyInformationBubble.CreateParticipantRemoved(Time.GetNowUnixTimestamp(), address,
                            this, messageService.Id.ToString(CultureInfo.InvariantCulture), fromId, userDeleted));
                    }
                    else if (addUser != null)
                    {
                        var userAdded = addUser.UserId.ToString(CultureInfo.InvariantCulture);
                        EventBubble(PartyInformationBubble.CreateParticipantAdded(Time.GetNowUnixTimestamp(), address,
                            this, messageService.Id.ToString(CultureInfo.InvariantCulture), fromId, userAdded));
                    }
                    else if (created != null)
                    {
                        EventBubble(PartyInformationBubble.CreateParticipantAdded(Time.GetNowUnixTimestamp(), address,
                            this, messageService.Id.ToString(CultureInfo.InvariantCulture), fromId,
                            _settings.AccountId.ToString(CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        Console.WriteLine("Unknown message service: " + ObjectDumper.Dump(update));
                    }
                }
                else
                {
                    Console.WriteLine("Unknown update: " + ObjectDumper.Dump(update));
                }
            }            
        }
            
        private void OnLongPollClientClosed(object sender, EventArgs e)
        {
            if (!_longPollerAborted)
            {
                Utils.DebugPrint("Looks like a long poll client closed itself internally. Restarting Telegram...");
                RestartTelegram(null);
            }
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

        private void DisconnectLongPollerIfPossible()
        {
            if (_longPollClient != null && _longPollClient.IsConnected)
            {
                _longPollerAborted = true;
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
                ProcessIncomingPayload(updates, false, client);
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
                if (!_dialogsInitiallyRetrieved)
                {
                    GetDialogs(client);
                    _dialogsInitiallyRetrieved = true;
                }
                GetConfig(client);
            }
            DebugPrint("Starting long poller...");
            if (_longPollClient != null)
            {
                _longPollClient.OnUpdateTooLong -= OnLongPollClientUpdateTooLong;
                _longPollClient.OnClosedInternally -= OnLongPollClientClosed;
            }
            _longPollerAborted = false;
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

        public override void SendBubble(Bubble b)
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
                using (var client = new FullClientDisposable(this))
                {
                    TelegramUtils.RunSynchronously(client.Client.Methods.AccountUpdateStatusAsync(
                        new AccountUpdateStatusArgs
                        {
                            Offline = !presenceBubble.Available
                        }));
                }
            }

            var typingBubble = b as TypingBubble;
            if (typingBubble != null)
            {
                var peer = GetInputPeer(typingBubble.Address, typingBubble.Party);
                using (var client = new FullClientDisposable(this))
                {
                    TelegramUtils.RunSynchronously(client.Client.Methods.MessagesSetTypingAsync(
                        new MessagesSetTypingArgs
                        {
                            Peer = peer,
                            Action = typingBubble.IsAudio ? 
                        (ISendMessageAction)new SendMessageRecordAudioAction() : (ISendMessageAction)new SendMessageTypingAction()
                        }));
                }
            }

            var textBubble = b as TextBubble;
            if (textBubble != null)
            {
                var peer = GetInputPeer(textBubble.Address, textBubble.Party);
                using (var client = new FullClientDisposable(this))
                {
                    TelegramUtils.RunSynchronously(client.Client.Methods.MessagesSendMessageAsync(new MessagesSendMessageArgs
                    {
                        Peer = peer,
                        Message = textBubble.Message,
                        RandomId = ulong.Parse(textBubble.IdService)
                    }));
                }
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
                using (var clientDisposable = new FullClientDisposable(this))
                {
                    var fullChat = GetFullChat(clientDisposable.Client, group.Address);
                    if (fullChat != null)
                    {
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
                    else
                    {
                        result(null);
                    }
                }
            });
        }

        public override Task GetBubbleGroupUnknownPartyParticipant(BubbleGroup group, string unknownPartyParticipant, Action<DisaParticipant> result)
        {
            return Task.Factory.StartNew(() =>
            {
                //TODO: this may not actually get title always.
                var name = GetTitle(unknownPartyParticipant, false);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    result(new DisaParticipant(name, unknownPartyParticipant));
                }
                else
                {
                    result(null);
                }
            });
        }

        public override Task GetBubbleGroupPartyParticipantPhoto(DisaParticipant participant, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
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
            Utils.DebugPrint("Phone book contacts have been updated! Sending information to Telegram servers...");
            var contacts = PhoneBook.PhoneBookContacts;
            var inputContacts = new List<IInputContact>();
            foreach (var contact in contacts)
            {
                foreach (var phoneNumber in contact.PhoneNumbers)
                {
                    inputContacts.Add(new InputPhoneContact
                    {
                        ClientId = ulong.Parse(contact.ContactId),
                        Phone = phoneNumber.Number,
                        FirstName = contact.FirstName,
                        LastName = contact.LastName,
                    });
                }
            }
            if (!inputContacts.Any())
            {
                Utils.DebugPrint("There are no input contacts!");
                return;
            }
            try
            {
                using (var client = new FullClientDisposable(this))
                {
                    TelegramUtils.RunSynchronously(client.Client.Methods.ContactsImportContactsAsync(
                        new ContactsImportContactsArgs
                        {
                            Contacts = inputContacts,
                            Replace = false,
                        }));
                    Utils.DebugPrint("Fetching newest dialogs...");
                    GetDialogs(client.Client);
                }
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to update contacts: " + ex);
            }
        }

        private IUser GetUser(List<IUser> users, string userId)
        {
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
            var peerUsers = _dialogs.Dialogs.ToList().Select(x => (x as Dialog).Peer).OfType<PeerUser>();
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
            using (var clientDisposable = new FullClientDisposable(this))
            {
                var inputUsers = users.Select(x => TelegramUtils.CastUserToInputUser(x)).Where(d => d != null).ToList();
                var updatedUsers = TelegramUtils.RunSynchronously(GetUsers(inputUsers, clientDisposable.Client));
                return updatedUsers;
            }
        }

        private long GetUpdatedLastOnline(string id)
        {
            foreach (var user in _dialogs.Users)
            {
                var userId = TelegramUtils.GetUserId(user);
                if (userId == id)
                {
                    var inputUser = TelegramUtils.CastUserToInputUser(user);
                    if (inputUser != null)
                    {
                        using (var clientDisposable = new FullClientDisposable(this))
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
            Func<DisaThumbnail, DisaThumbnail> cache = thumbnail =>
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
            if (group)
            {
                foreach (var chat in _dialogs.Chats)
                {
                    if (id == TelegramUtils.GetChatId(chat))
                    {
                        var fileLocation = TelegramUtils.GetChatThumbnailLocation(chat, small);
                        if (fileLocation == null)
                        {
                            return cache(null);
                        }
                        else
                        {
                            var bytes = FetchFileBytes(fileLocation);
                            return cache(new DisaThumbnail(this, bytes, id));
                        }
                    }
                }
            }
            else
            {
                Func<IUser, DisaThumbnail> getThumbnail = user =>
                {
                    var fileLocation = TelegramUtils.GetUserPhotoLocation(user, small);
                    if (fileLocation == null)
                    {
                        return cache(null);
                    }
                    else
                    {
                        var bytes = FetchFileBytes(fileLocation);
                        return cache(new DisaThumbnail(this, bytes, id));
                    }
                };
                foreach (var user in _dialogs.Users)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (userId == id)
                    {
                        return getThumbnail(user);
                    }
                }
                foreach (var fullChat in _dialogs.FullChats)
                {
                    var messagesChatFull = fullChat as MessagesChatFull;
                    if (messagesChatFull != null)
                    {
                        foreach (var user in messagesChatFull.Users)
                        {
                            var userId = TelegramUtils.GetUserId(user);
                            if (userId == id)
                            {
                                return getThumbnail(user);
                            }
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
                using (var clientDisposable = new FullClientDisposable(this))
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
 
        private MessagesChatFull GetFullChat(string address)
        {
            using (var disposable = new FullClientDisposable(this))
            {
                return GetFullChat(disposable.Client, address);
            }
        }

        private MessagesChatFull GetFullChat(TelegramClient client, string address)
        {
            var id = uint.Parse(address);
            foreach (var iChatFull in _dialogs.FullChats)
            {
                var chatFull = iChatFull as MessagesChatFull;
                if (chatFull != null)
                {
                    var chatFull2 = chatFull.FullChat as ChatFull;
                    if (id == chatFull2.Id)
                    {
                        return chatFull;
                    }
                }
            }
            if (_dialogs.FullChatFailures.FirstOrDefault(x => x == address) != null)
            {
                return null;
            }
            else
            {
                return FetchAndCacheFullChat(client, _dialogs, address);
            }
        }

        private void RemoveFullChat(string address)
        {
            var id = uint.Parse(address);
            foreach (var iChatFull in _dialogs.FullChats)
            {
                var chatFull = iChatFull as MessagesChatFull;
                if (chatFull != null)
                {
                    var chatFull2 = chatFull.FullChat as ChatFull;
                    if (id == chatFull2.Id)
                    {
                        _dialogs.FullChats.Remove(iChatFull);
                        break;
                    }
                }
            }
            var chatFailure = _dialogs.FullChatFailures.FirstOrDefault(x => address == x);
            if (chatFailure != null)
            {
                _dialogs.FullChatFailures.Remove(chatFailure);
            }
        }

        private void FetchFullChatsForParties(TelegramClient client, CachedDialogs dialogs)
        {
            foreach (var group in BubbleGroupManager.FindAll(this).Where(x => x.IsParty))
            {
                FetchAndCacheFullChat(client, dialogs, group.Address);
            }
        }

        private static MessagesChatFull FetchAndCacheFullChat(TelegramClient client, 
            CachedDialogs dialogs, string address)
        {
            var fullChat = FetchFullChat(client, address);
            if (fullChat != null)
            {
                dialogs.FullChats.Add(fullChat);
            }
            else
            {
                dialogs.FullChatFailures.Add(address);   
            }
            return fullChat;
        }

        private static MessagesChatFull FetchFullChat(TelegramClient client, string address)
        {
            var fullChat = (MessagesChatFull)TelegramUtils.RunSynchronously(client.Methods.MessagesGetFullChatAsync(new MessagesGetFullChatArgs
            {
                ChatId = uint.Parse(address) 
            }));
            return fullChat;
        }

        private void GetDialogs(TelegramClient client)
        {
            DebugPrint("Fetching conversations");
            var masterDialogs = new CachedDialogs();
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
                masterDialogs.Chats.AddRange(dialogs.Chats);
                masterDialogs.Dialogs.AddRange(dialogs.Dialogs);
                masterDialogs.Messages.AddRange(dialogs.Messages);
                masterDialogs.Users.AddRange(dialogs.Users);
            }
            else if (dialogsSlice != null)
            {
                masterDialogs.Chats.AddRange(dialogsSlice.Chats);
                masterDialogs.Dialogs.AddRange(dialogsSlice.Dialogs);
                masterDialogs.Messages.AddRange(dialogsSlice.Messages);
                masterDialogs.Users.AddRange(dialogsSlice.Users);
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
            FetchFullChatsForParties(client, masterDialogs);
            _dialogs = masterDialogs;
            DebugPrint("Obtained conversations.");
        }

    }
}

