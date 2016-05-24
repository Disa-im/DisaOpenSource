using System;
using SharpMTProto.Schema;
using SharpMTProto.Messaging.Handlers;
using SharpTL;
using System.IO;
using System.IO.Compression;
using Disa.Framework.Telegram;
using Nito.AsyncEx;

namespace SharpMTProto.Messaging.Handlers
{
    public class GzipPackedHandler : ResponseHandler<GzipPacked>
    {
        private readonly TLRig _tlRig;
        private readonly UpdatesHandler _handler;

        public GzipPackedHandler(TLRig tlRig, UpdatesHandler handler)
        {
            _tlRig = tlRig;
            _handler = handler;
        }
            
        protected override System.Threading.Tasks.Task HandleInternalAsync(IMessage responseMessage)
        {
            var gzipPacked = responseMessage.Body as GzipPacked;

            using (var uncompressedStream = new MemoryStream())
            {
                using (var compressedStream = new MemoryStream(gzipPacked.PackedData))
                {
                    using (var gzip = new GZipStream(compressedStream, CompressionMode.Decompress))
                    {
                        gzip.CopyTo(uncompressedStream);
                        var uncompressed = uncompressedStream.ToArray();
                        using (var streamer = new TLStreamer(uncompressed))
                        {
                            var newResult = _tlRig.Deserialize(streamer);
                            if (newResult is SharpTelegram.Schema.IUpdates)
                            {
                                return _handler.HandleAsync(new Message { Body = newResult });
                            }
                        }
                    }
                }
            }

            return TaskConstants.Completed;
        }
    }
}

