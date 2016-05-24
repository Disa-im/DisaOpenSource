// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RpcResultHandler.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Nito.AsyncEx;
using SharpMTProto.Schema;
using System;
using SharpTL;
using System.IO;
using System.IO.Compression;

namespace SharpMTProto.Messaging.Handlers
{
    public class RpcResultHandler : ResponseHandler<IRpcResult>
    {
        private readonly IRequestsManager _requestsManager;
        private readonly TLRig _tlRig;

        public RpcResultHandler(IRequestsManager requestsManager, TLRig tlRig)
        {
            _requestsManager = requestsManager;
            _tlRig = tlRig;
        }

        protected override Task HandleInternalAsync(IMessage responseMessage)
        {
            var rpcResult = (IRpcResult) responseMessage.Body;
            object result = rpcResult.Result;

            IRequest request = _requestsManager.Get(rpcResult.ReqMsgId);
            if (request == null)
            {
                Console.WriteLine(
                    string.Format(
                        "Ignored response of type '{1}' for not existed request with MsgId: 0x{0:X8}.",
                        rpcResult.ReqMsgId,
                        result.GetType()));
                return TaskConstants.Completed;
            }

            var rpcError = result as IRpcError;
            var gzipPacked = result as GzipPacked;
            if (rpcError != null)
            {
                request.SetException(new RpcErrorException(rpcError));
            }
            else if (gzipPacked != null)
            {
                using (var uncompressedStream = new MemoryStream())
                {
                    using (var compressedStream = new MemoryStream(gzipPacked.PackedData))
                    {
                        using (var gzip = new GZipStream(compressedStream, CompressionMode.Decompress))
                        {
                            gzip.CopyTo(uncompressedStream);
                            var uncompressed = uncompressedStream.ToArray();
                            Console.WriteLine(Convert.ToBase64String(uncompressed));
                            using (var streamer = new TLStreamer(uncompressed))
                            {
                                var newResult = _tlRig.Deserialize(streamer);
                                request.SetResponse(newResult);
                            }
                        }
                    }
                }
            }
            else
            {
                request.SetResponse(result);
            }
            return TaskConstants.Completed;
        }
    }
}
