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

//RESEARCH TOPICS:
//1) We're sending everything with InputContact/PeerUser ... does the server care if the user is not on our contacts list?

namespace Disa.Framework.Telegram
{
    [ServiceInfo("Telegram", true, false, false, false, false, typeof(TelegramSettings), 
        ServiceInfo.ProcedureType.ConnectAuthenticate, typeof(TextBubble), typeof(PresenceBubble))]
    public partial class Telegram : Service, IVisualBubbleServiceId, ITerminal
    {
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

        private TelegramClient _fullClient
        {
            get
            {
                if (_fullClientInternal != null && _fullClientInternal.IsConnected)
                {
                    return _fullClientInternal;
                }
                var transportConfig = 
                    new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                _fullClientInternal = new TelegramClient(transportConfig, 
                    new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo);
                var result = RunSynchronously(_fullClientInternal.Connect());
                if (result != MTProtoConnectResult.Success)
                {
                    throw new Exception("Failed to connect: " + result);
                }
                SetFullClientPingDelayDisconnect();
                return _fullClientInternal;
            }
        }

        private async void SetFullClientPingDelayDisconnect()
        {
            if (_fullClient == null || !_fullClient.IsConnected)
            {
                return;   
            }
            IPong iPong;
            if (_hasPresence)
            {
                DebugPrint("Telling full client that it can forever stay alive.");

                iPong = await _fullClient.ProtoMethods.PingDelayDisconnectAsync(new PingDelayDisconnectArgs
                {
                    PingId = GetRandomId(),
                    DisconnectDelay = uint.MaxValue,
                });
            }
            else
            {
                DebugPrint("Telling full client that it can only stay alive for a minute.");
                iPong = await _fullClient.ProtoMethods.PingDelayDisconnectAsync(new PingDelayDisconnectArgs
                {
                    PingId = GetRandomId(),
                    DisconnectDelay = 60,
                });
            }
        }

        private void DisconnectFullClientIfPossible()
        {
            if (_fullClient != null && _fullClient.IsConnected)
            {
                try
                {
                    RunSynchronously(_fullClient.Disconnect());
                }
                catch (Exception ex)
                {
                    DebugPrint("Failed to disconnect full client: " + ex);
                }
            }
        }

        private void DisconnectLongPollerIfPossible()
        {
            if (_longPollClient != null && _longPollClient.IsConnected)
            {
                try
                {
                    RunSynchronously(_longPollClient.Disconnect());
                }
                catch (Exception ex)
                {
                    DebugPrint("Failed to disconnect full client: " + ex);
                }
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
                var authInfo = RunSynchronously(FetchNewAuthentication(DefaultTransportConfig));
                using (var client = new TelegramClient(DefaultTransportConfig, 
                    new ConnectionConfig(authInfo.AuthKey, authInfo.Salt), AppInfo))
                {
                    RunSynchronously(client.Connect());
                    var nearestDcId = (NearestDc)RunSynchronously(client.Methods.HelpGetNearestDcAsync(new HelpGetNearestDcArgs{}));
                    var config = (Config)RunSynchronously(client.Methods.HelpGetConfigAsync(new HelpGetConfigArgs{ }));
                    var dcOption = config.DcOptions.OfType<DcOption>().FirstOrDefault(x => x.Id == nearestDcId.NearestDcProperty);
                    settings.NearestDcId = nearestDcId.NearestDcProperty;
                    settings.NearestDcIp = dcOption.IpAddress;
                    settings.NearestDcPort = (int)dcOption.Port;
                }
                service.DebugPrint("Generating authentication on nearest DC...");
                var authInfo2 = RunSynchronously(FetchNewAuthentication(
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
                    RunSynchronously(client.Connect());

                    if (!call)
                    {
                        try
                        {
                            var result = RunSynchronously(client.Methods.AuthSendCodeAsync(new AuthSendCodeArgs
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
                    var result2 = (bool)RunSynchronously(client.Methods.AuthSendCallAsync(new AuthSendCallArgs
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
                    RunSynchronously(client.Connect());

                    try
                    {
                        IAuthAuthorization iresult = null;
                        if (signIn)
                        {
                            iresult = RunSynchronously(client.Methods.AuthSignInAsync(new AuthSignInArgs
                                {
                                    PhoneNumber = number,
                                    PhoneCodeHash = codeHash,
                                    PhoneCode = code,
                                }));
                        }
                        else
                        {
                            iresult = RunSynchronously(client.Methods.AuthSignUpAsync(new AuthSignUpArgs
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

        private static T RunSynchronously<T>(Task<T> task)
        {
            try
            {
                task.Wait();
                return task.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.Flatten().InnerException;
            }
        }

        private static void RunSynchronously(Task task)
        {
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.Flatten().InnerException;
            }
        }

        private uint GetNearestDc()
        {
            var nearestDc = (NearestDc)RunSynchronously(
                _fullClient.Methods.HelpGetNearestDcAsync(new HelpGetNearestDcArgs{}));
            return nearestDc.NearestDcProperty;
        }

        private Tuple<string, uint> GetDcIPAndPort(uint id)
        {
            var config = (Config)RunSynchronously(_fullClient.Methods.HelpGetConfigAsync(new HelpGetConfigArgs{ }));
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

        public override void Connect(WakeLock wakeLock)
        {
            var sessionId = GetRandomId();
            var transportConfig = 
                new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
            using (var client = new TelegramClient(transportConfig, 
                                    new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
            {
                var result = RunSynchronously(client.Connect());
                if (result != MTProtoConnectResult.Success)
                {
                    throw new Exception("Failed to connect: " + result);
                }   
                DebugPrint("Registering long poller...");
                var registerDeviceResult = RunSynchronously(client.Methods.AccountRegisterDeviceAsync(
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
            }
            DebugPrint("Starting long poller...");
            _longPollClient = new TelegramClient(transportConfig, 
                new ConnectionConfig(_settings.AuthKey, _settings.Salt) { SessionId = sessionId }, AppInfo);
            RunSynchronously(_longPollClient.Connect());
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
                var users = await GetUsers(BubbleGroupManager.FindAll(this).Where(x => !x.IsParty).Select(x => x.Address).ToList());
                foreach (var user in users)
                {
                    EventBubble(new PresenceBubble(Time.GetNowUnixTimestamp(), Bubble.BubbleDirection.Incoming, 
                        TelegramUtils.GetUserId(user), false, this, TelegramUtils.GetAvailable(user)));
                }
                await _fullClient.Methods.AccountUpdateStatusAsync(new AccountUpdateStatusArgs
                    {
                        Offline = !presenceBubble.Available
                    });
            }


            var textBubble = b as TextBubble;
            if (textBubble != null)
            {
                var message = (MessagesSentMessage)RunSynchronously(_fullClient.Methods.MessagesSendMessageAsync(new MessagesSendMessageArgs
                    {
                        Peer = new InputPeerContact  { UserId = uint.Parse(textBubble.Address) },
                        Message = textBubble.Message,
                        RandomId = ulong.Parse(NextMessageId)
                    }));
            }
        }

        public override bool BubbleGroupComparer(string first, string second)
        {
            return first == second;
        }

        public override Task GetBubbleGroupLegibleId(BubbleGroup group, Action<string> result)
        {
            throw new NotImplementedException();
        }

        private async Task<List<IUser>> GetUsers(List<string> userIds)
        {
            var response = await _fullClient.Methods.UsersGetUsersAsync(new UsersGetUsersArgs
                {
                    Id = userIds.Select(x => 
                        new InputUserContact
                        {
                            UserId = uint.Parse(x)
                        }).Cast<IInputUser>().ToList()
                });
            return response;
        }

        private async Task<IUser> GetUser(string userId)
        {
            return (await GetUsers(new List<string> { userId })).First();
        }

        public override Task GetBubbleGroupName(BubbleGroup group, Action<string> result)
        {
            return Task.Factory.StartNew(async () =>
                {
                    if (group.IsParty)
                    {
                        //TODO:
                    }
                    else
                    {
                        result(TelegramUtils.GetNameForSoloConversation(await GetUser(group.Address)));
                    }
                });
        }

        public override Task GetBubbleGroupPhoto(BubbleGroup group, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(null);
            });
        }

        public override Task GetBubbleGroupPartyParticipants(BubbleGroup group, Action<DisaParticipant[]> result)
        {
            throw new NotImplementedException();
        }

        public override Task GetBubbleGroupUnknownPartyParticipant(BubbleGroup group, string unknownPartyParticipant, Action<DisaParticipant> result)
        {
            throw new NotImplementedException();
        }

        public override Task GetBubbleGroupPartyParticipantPhoto(DisaParticipant participant, Action<DisaThumbnail> result)
        {
            throw new NotImplementedException();
        }

        public override Task GetBubbleGroupLastOnline(BubbleGroup group, Action<long> result)
        {
            return Task.Factory.StartNew(async () =>
                {
                    result(TelegramUtils.GetLastSeenTime(await GetUser(group.Address)));
                });
        }

        public void AddVisualBubbleIdServices(VisualBubble bubble)
        {
            bubble.IdService = Time.GetNowUnixTimestamp().ToString(CultureInfo.InvariantCulture);
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
    }

    public class TelegramSettings : DisaSettings
    {
        public byte[] AuthKey { get; set; }
        public ulong Salt { get; set; }
        public uint NearestDcId { get; set; }
        public string NearestDcIp { get; set; }
        public int NearestDcPort { get; set; }
    }

    public class TelegramMutableSettings : DisaMutableSettings
    {
    }
}

