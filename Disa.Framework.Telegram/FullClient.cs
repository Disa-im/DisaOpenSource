using System;
using SharpTelegram;
using SharpMTProto.Transport;
using SharpMTProto;
using SharpMTProto.Schema;
using System.Threading.Tasks;
using SharpTelegram.Schema.Layer18;
using System.Collections.Generic;

namespace Disa.Framework.Telegram
{
    public partial class Telegram
    {
        //TODO:
        private static TelegramAppInfo AppInfo = new TelegramAppInfo
        {
            ApiId = 19606,
            DeviceModel = "LG",
            SystemVersion = "5.0",
            AppVersion = "0.8.2",
            LangCode = PhoneBook.Language, //TODO: works?
        };

        private TelegramClient _fullClientInternal;
        private WakeLockBalancer.GracefulWakeLock _fullClientHeartbeat;
        private readonly object _fullClientLock = new object();

        private bool IsFullClientConnected
        {
            get
            {
                lock (_fullClientLock)
                {
                    return _fullClientInternal != null && _fullClientInternal.IsConnected;
                }
            }
        }

        private TelegramClient _fullClient
        {
            get
            {
                lock (_fullClientLock)
                {
                    if (_fullClientInternal != null && _fullClientInternal.IsConnected)
                    {
                        return _fullClientInternal;
                    }
                    Console.WriteLine(System.Environment.StackTrace);
                    DebugPrint("!!!!!! Full client is not connected. Starting it up!");
                    var transportConfig = 
                        new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                    if (_fullClientInternal != null)
                    {
                        _fullClientInternal.OnUpdateState -= OnFullClientUpdateState;
                        _fullClientInternal.OnUpdate -= OnFullClientUpdate;
                        _fullClientInternal.OnUpdateTooLong -= OnFullClientUpdateTooLong;
                    }
                    _fullClientInternal = new TelegramClient(transportConfig, 
                        new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo);
                    _fullClientInternal.OnUpdateState += OnFullClientUpdateState;
                    _fullClientInternal.OnUpdate += OnFullClientUpdate;
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

        private void OnFullClientUpdateTooLong(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                FetchState(_fullClient);
            });
        }

        private void OnFullClientUpdateState(object sender, SharpMTProto.Messaging.Handlers.UpdatesHandler.State s)
        {
            Task.Factory.StartNew(() =>
            {
                SaveState(s.Date, s.Pts, s.Qts, s.Seq);
            });
        }

        private void OnFullClientUpdate(object sender, List<object> updates)
        {
            ProcessIncomingPayload(updates, true);
        }

        private void SetFullClientPingDelayDisconnect()
        {
            if (_hasPresence)
            {
                DebugPrint("Telling full client that it can forever stay alive.");
                PingDelay(_fullClient, uint.MaxValue);
                ScheduleFullClientPing();
            }
            else
            {
                if (!IsFullClientConnected)
                {
                    return;   
                }
                DebugPrint("Telling full client that it can only stay alive for a minute.");
                PingDelay(_fullClient, 60);
                RemoveFullClientPingIfPossible();
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

//        private uint GetNearestDc()
//        {
//            var nearestDc = (NearestDc)TelegramUtils.RunSynchronously(
//                _fullClient.Methods.HelpGetNearestDcAsync(new HelpGetNearestDcArgs{}));
//            return nearestDc.NearestDcProperty;
//        }
//
//        private Tuple<string, uint> GetDcIPAndPort(uint id)
//        {
//            var config = (Config)TelegramUtils.RunSynchronously(_fullClient.Methods.HelpGetConfigAsync(new HelpGetConfigArgs{ }));
//            var dcOption = config.DcOptions.OfType<DcOption>().FirstOrDefault(x => x.Id == id);
//            return Tuple.Create(dcOption.IpAddress, dcOption.Port);
//        }

        private class FullClientDisposable : IDisposable
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

            public FullClientDisposable(Telegram telegram)
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
}

