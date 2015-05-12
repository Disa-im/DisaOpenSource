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
    public class Telegram : Service, IVisualBubbleServiceId
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

            return true;
        }

        public override bool InitializeDefault()
        {
            return false;
        }

        public static AuthInfo FetchNewAuthentication()
        {
            var authKeyNegotiater = MTProtoClientBuilder.Default.BuildAuthKeyNegotiator(DefaultTransportConfig);
            authKeyNegotiater.KeyChain.Add(RSAPublicKey.Get());
            var rask = authKeyNegotiater.CreateAuthKey();
            rask.Wait();
            return rask.Result;
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

        public void ChangeDcIfNeeded()
        {
            var nearestDc = GetNearestDc();
            if (_mutableSettings.NearestDcId != nearestDc) // what if the first DC is zero? fix this case
            {
                var ipAndPort = GetDcIPAndPort(nearestDc);
                _mutableSettings.NearestDcIp = ipAndPort.Item1;
                _mutableSettings.NearestDcPort = ipAndPort.Item2;

                MutableSettingsManager.Save(_mutableSettings);
                throw new ServiceSpecialRestartException("Changing DCs");
            }
        }

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
            var transportConfig = DefaultTransportConfig;
            if (_mutableSettings.NearestDcIp != null)
            {
                transportConfig = 
                    new TcpClientTransportConfig(_mutableSettings.NearestDcIp, (int)_mutableSettings.NearestDcPort);
            }
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
            ChangeDcIfNeeded();
//
//            var lol2 = _client.Methods.HelpGetConfigAsync(new SharpTelegram.Schema.Layer18.HelpGetConfigArgs
//            {
//            });
//            lol2.Wait();
//            var sdasd = (Config)lol2.Result;
//            int ihz = 5;
//
////            var b = _client.Methods.HelpGetNearestDcAsync(new SharpTelegram.Schema.Layer18.HelpGetNearestDcArgs
////            {
////            });
//
////            b.Wait();
////            var lol2 = b.Result;
//
//            var x = _client.Methods.AuthSendCodeAsync(new SharpTelegram.Schema.Layer18.AuthSendCodeArgs
//            {
//                PhoneNumber = "16043170693",
//                SmsType = 0,
//                ApiId = 19606,
//                ApiHash = "f8f2562579817ddcec76a8aae4cd86f6",
//                LangCode = "en"
//            });
//            x.Wait();
//            var lol = x.Result;
//            int ih = 5;
        }

        public override void Disconnect()
        {
            _client.Disconnect();
        }

        public override string GetIcon(bool large)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Bubble> ProcessBubbles()
        {
            throw new NotImplementedException();
        }

        private static string Reverse( string s )
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse( charArray );
            return new string( charArray );
        }

        public override void SendBubble(Bubble b)
        {
            var textBubble = b as TextBubble;
            if (textBubble != null)
            {
                Utils.Delay(2000).Wait();
                Platform.ScheduleAction(1, new WakeLockBalancer.ActionObject(() =>
                {
                    EventBubble(new TextBubble(Time.GetNowUnixTimestamp(), Bubble.BubbleDirection.Incoming,
                        textBubble.Address, null, false, this, Reverse(textBubble.Message)));
                }, WakeLockBalancer.ActionObject.ExecuteType.TaskWithWakeLock));
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
    }

    public class TelegramMutableSettings : DisaMutableSettings
    {
        public uint NearestDcId { get; set; }
        public string NearestDcIp { get; set; }
        public uint NearestDcPort { get; set; }
    }
}

