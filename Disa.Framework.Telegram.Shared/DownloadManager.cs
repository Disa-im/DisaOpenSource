using System;
using System.IO;
using System.Linq;
using SharpMTProto;
using SharpMTProto.Transport;
using SharpTelegram;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram
    {
        private class DownloadManager : IDisposable
        {
            private Document _document;
            private Telegram _telegram;
            private TelegramClient _client;
            private uint _chunkSize = 524288;
            private uint _fileSize;

            public DownloadManager(Document document, Telegram telegram)
            {
                _document = document;
                _fileSize = document.Size;
                _telegram = telegram;
                int dcId = (int)_document.DcId;
                //initialize the dc we want to download from
                if (_document.DcId != _telegram.Settings.NearestDcId)
                {
                    // if its not the nearest dc, it could be unintialized. so lets check and init it.
                    var dc = DcDatabase.Get(dcId);
                    if (dc == null)
                    {
                        _telegram.GetClient(dcId);
                        //after this our dc configuration is ready, the connection should die out in a 60 secs.
                    }
                    var dcCached = DcDatabase.Get(dcId);
                    var dcOption = _telegram.Config.DcOptions.Cast<DcOption>().FirstOrDefault(x => x.Id == dcId);
                    var tcpConfig = new TcpClientTransportConfig(dcOption.IpAddress, (int)dcOption.Port);
                    var authInfo = new SharpMTProto.Authentication.AuthInfo(dcCached.Key,
                           BitConverter.ToUInt64(dcCached.Salt, 0));
                    _client = new TelegramClient(tcpConfig,
                        new ConnectionConfig(authInfo.AuthKey, authInfo.Salt), AppInfo);
                    TelegramUtils.RunSynchronously(_client.Connect());
                }
                else
                {
                    var tcpConfig = new TcpClientTransportConfig(_telegram.Settings.NearestDcIp, (int)_telegram.Settings.NearestDcPort);
                    var authInfo = new SharpMTProto.Authentication.AuthInfo(_telegram.Settings.AuthKey,
                                                                            _telegram.Settings.Salt);
                    _client = new TelegramClient(tcpConfig,
                        new ConnectionConfig(authInfo.AuthKey, authInfo.Salt), AppInfo);
                    TelegramUtils.RunSynchronously(_client.Connect());
                }
            }

            public uint DownloadDocument(FileStream fileStream, uint currentOffset, Action<int> progress)
            {
                int maxPackets = 60;
                int currentPackets = 0;
                _telegram.PingDelay(_client, uint.MaxValue);
                while (currentPackets < maxPackets)
                {
                    var bytes = _telegram.FetchDocumentBytes(_client,_document, currentOffset,
                                                _chunkSize);
                    if (bytes.Length == 0)
                    {
                        break;
                    }
                    fileStream.Write(bytes, 0, bytes.Length);
                    float currentProgress = currentOffset / (float)_fileSize;

                    progress((int)(currentProgress * 100));
                    currentOffset += _chunkSize;
                    currentPackets++;
                }
                return currentOffset;
            }

            public void Dispose()
            {
                _client.Dispose();
            }
        }
    }
}
