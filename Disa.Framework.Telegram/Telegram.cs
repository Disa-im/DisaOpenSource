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

namespace Disa.Framework.Telegram
{
    [ServiceInfo("Telegram", true, false, false, false, false, typeof(TelegramSettings), 
        ServiceInfo.ProcedureType.ConnectAuthenticate, typeof(TextBubble))]
    public class Telegram : Service, IVisualBubbleServiceId, ITerminal
    {
        private static TcpClientTransportConfig DefaultTransportConfig = 
            new TcpClientTransportConfig("149.154.167.50", 443);

        private static TelegramAppInfo AppInfo = new TelegramAppInfo
        {
            ApiId = 19606,
            DeviceModel = "LG",
            SystemVersion = "5.0",
            AppVersion = "0.8.2",
            LangCode = "en",
        };

        private TelegramSettings _settings;
        private TelegramMutableSettings _mutableSettings;

        private TelegramClient _client;

        public override bool Initialize(DisaSettings settings)
        {
            _settings = settings as TelegramSettings;
            _mutableSettings = MutableSettingsManager.Load<TelegramMutableSettings>();

            if (_settings.AuthKey == null || _settings.Salt == null)
            {
                return false;
            }

            return true;
        }

        public override bool InitializeDefault()
        {
            return false;
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
                            var result = (AuthSentCode)await client.Methods.AuthSendCodeAsync(new AuthSendCodeArgs
                            {
                                PhoneNumber = number,
                                SmsType = 0,
                                ApiId = AppInfo.ApiId,
                                ApiHash = "f8f2562579817ddcec76a8aae4cd86f6",
                                LangCode = "en"
                            });
                            DebugPrint(result.ToString());
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
                            DebugPrint(result.ToString());
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
                            DebugPrint(result.ToString());
                        }
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

        private T RynSynchronously<T>(Task<T> task)
        {
            task.Wait();
            return task.Result;
        }

        private uint GetNearestDc()
        {
            var nearestDc = (NearestDc)RynSynchronously(
                _client.Methods.HelpGetNearestDcAsync(new HelpGetNearestDcArgs{}));
            return nearestDc.NearestDcProperty;
        }

        private Tuple<string, uint> GetDcIPAndPort(uint id)
        {
            var config = (Config)RynSynchronously(_client.Methods.HelpGetConfigAsync(new HelpGetConfigArgs{ }));
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
            var transportConfig = 
                new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
            _client = new TelegramClient(transportConfig, 
                new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo);
            using (new WakeLock.TemporaryFree(wakeLock))
            {
                var task = _client.Connect();
                task.Wait();
                var result = task.Result;
                if (result != MTProtoConnectResult.Success)
                {
                    throw new Exception("Failed to connect: " + task.Result);
                }
            }
        }

        public override void Disconnect()
        {
            _client.Dispose();
        }

        public override string GetIcon(bool large)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Bubble> ProcessBubbles()
        {
            throw new NotImplementedException();
        }

        public override void SendBubble(Bubble b)
        {
        }

        public override bool BubbleGroupComparer(string first, string second)
        {
            return first == second;
        }

        public override Task GetBubbleGroupLegibleId(BubbleGroup group, Action<string> result)
        {
            throw new NotImplementedException();
        }

        public override Task GetBubbleGroupName(BubbleGroup group, Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(group.Address);
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
            throw new NotImplementedException();
        }

        public void AddVisualBubbleIdServices(VisualBubble bubble)
        {
        }

        public bool DisctinctIncomingVisualBubbleIdServices()
        {
            return true;
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

