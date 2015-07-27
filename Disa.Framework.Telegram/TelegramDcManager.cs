using System;
using SQLite;
using System.Collections.Generic;
using SharpTelegram;
using SharpTelegram.Schema.Layer18;
using System.Linq;
using SharpMTProto.Transport;
using SharpMTProto;
using SharpMTProto.Schema;
using System.IO;

namespace Disa.Framework.Telegram
{
    public partial class Telegram
    {
        public class TelegramDc
        {
            [PrimaryKey]
            public int Dc { get; set; }

            public byte[] Key { get; set; }

            public byte[] Salt { get; set; }
        }
            
        private class DcDatabase
        {
            private static object _lock = new object();

            private static string Location
            {
                get
                {
                    var settingsPath
                    = Platform.GetSettingsPath();
                    var locationPath = Path.Combine(settingsPath, "TelegramDcs.db");
                    return locationPath;
                }
            }

            public static void Set(TelegramDc dc)
            {
                lock (_lock)
                {
                    using (var db = new SqlDatabase<TelegramDc>(Location))
                    {
                        foreach (var entry in db.Store.Where(x => x.Dc == dc.Dc))
                        {
                            entry.Key = dc.Key;
                            entry.Salt = dc.Salt;
                            db.Update(entry);
                            return;
                        }
                            
                        db.Add(dc);
                    }
                }
            }

            public static TelegramDc Get(int dc)
            {
                lock (_lock)
                {
                    using (var db = new SqlDatabase<TelegramDc>(Location))
                    {
                        foreach (var entry in db.Store.Where(x => x.Dc == dc))
                        {
                            return entry;
                        }
                    }
                }
                return null;
            }
        }

        private readonly Dictionary<int, TelegramClient> _activeClients = new Dictionary<int, TelegramClient>();
        private readonly Dictionary<int, object> _spinUpLocks = new Dictionary<int, object>();

        public TelegramClient GetClient(int dc)
        {
            if (dc == _settings.NearestDcId)
                throw new Exception("Cannot spin up a client that uses the primary DC!");

            var client = GetClientInternal(dc);
            if (client != null)
                return client;

            return SpinUpClient(dc);
        }

        private TelegramClient GetClientInternal(int dc)
        {
            lock (_activeClients)
            {
                if (_activeClients.ContainsKey(dc))
                {
                    var client = _activeClients[dc];

                    PingDelay(client, 60);

                    return client;
                }
            }
            return null;
        }

        private TelegramClient SpinUpClient(int dc)
        {
            object dcLock;

            lock (_spinUpLocks)
            {
                if (_spinUpLocks.ContainsKey(dc))
                {
                    dcLock = _spinUpLocks[dc];
                }
                else
                {
                    dcLock = new object();
                    _spinUpLocks[dc] = dcLock;
                }
            }

            lock (dcLock)
            {
                var client = GetClientInternal(dc);
                if (client != null)
                    return client;

                if (_config == null)
                {
                    DebugPrint("Config is null. Unable to resolve DC information.");
                    return null;
                }

                var dcOption = _config.DcOptions.Cast<DcOption>().FirstOrDefault(x => x.Id == dc);

                if (dcOption == null)
                {
                    DebugPrint("Unable to find DC for DC ID: " + dc);
                    return null;
                }

                var dcCached = DcDatabase.Get(dc);

                var transportConfig = 
                    new TcpClientTransportConfig(dcOption.IpAddress, (int)dcOption.Port);

                SharpMTProto.Authentication.AuthInfo authInfo;
                AuthExportedAuthorization exportedAuth = null;

                if (dcCached == null)
                {
                    DebugPrint("Looks like we'll have to authenticate a new connection to: "
                    + ObjectDumper.Dump(dcOption));

                    DebugPrint(">>>>>>>> Exporting auth...");

                    using (var clientDisposable = new TelegramClientDisposable(this))
                    {
                        exportedAuth = (AuthExportedAuthorization)TelegramUtils.RunSynchronously(clientDisposable.Client.Methods.AuthExportAuthorizationAsync(
                            new SharpTelegram.Schema.Layer18.AuthExportAuthorizationArgs
                            {
                                DcId = (uint)dc,
                            }));
                    }

                    DebugPrint(">>>>>>> Got exported auth.");

                    if (exportedAuth == null)
                    {
                        DebugPrint("Exported auth is null for some weird reason. DC ID: " + dc);
                        return null;
                    }

                    DebugPrint(">>>>>>> Fetching new authentication...");

                    authInfo = TelegramUtils.RunSynchronously(FetchNewAuthentication(transportConfig));
                }
                else
                {
                    authInfo = new SharpMTProto.Authentication.AuthInfo(dcCached.Key,
                        BitConverter.ToUInt64(dcCached.Salt, 0));
                }

                DebugPrint(">>>>>>>> Starting new client...");

                var newClient = new TelegramClient(transportConfig, 
                    new ConnectionConfig(authInfo.AuthKey, authInfo.Salt), AppInfo);

                newClient.OnClosedInternally += (sender, e) =>
                {
                    DebugPrint("Removing connection to DC: "+ dc);
                    lock (_activeClients)
                    {
                        _activeClients.Remove(dc);
                    }
                };

                var result = TelegramUtils.RunSynchronously(newClient.Connect());
                if (result != MTProtoConnectResult.Success)
                {
                    DebugPrint("Failed to connect to DC: " + dc + ": " + result);
                    return null;
                }

                if (exportedAuth != null)
                {
                    TelegramUtils.RunSynchronously(newClient.Methods.AuthImportAuthorizationAsync(new AuthImportAuthorizationArgs
                        {
                            Id = exportedAuth.Id,
                            Bytes = exportedAuth.Bytes,
                        }));
                }

                PingDelay(client, 60);

                lock (_activeClients)
                {
                    _activeClients[dc] = newClient;
                }

                if (dcCached == null)
                {
                    DcDatabase.Set(new TelegramDc
                    {
                        Dc = dc,
                        Key = authInfo.AuthKey,
                        Salt = BitConverter.GetBytes(authInfo.Salt),
                    });
                }

                return newClient;
            }
        }
    }
}

