// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequestsManager.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace SharpMTProto.Messaging
{
    public interface IRequestsManager
    {
        void Add(IRequest request);
        IRequest Get(ulong messageId);
        IRequest GetFirstOrDefault(object response, bool includeRpc = false);
        void Change(ulong newMessageId, ulong oldMessageId);
    }

    public class RequestsManager : IRequestsManager
    {
        private readonly SortedDictionary<ulong, IRequest> _requests = new SortedDictionary<ulong, IRequest>();

        public void Add(IRequest request)
        {
            _requests.Add(request.Message.MsgId, request);
        }

        public void Change(ulong newMessageId, ulong oldMessageId)
        {
            _requests.Add(newMessageId, _requests[oldMessageId]);
            _requests.Remove(oldMessageId);
        }

        public IRequest Get(ulong messageId)
        {
            IRequest request;
            return _requests.TryGetValue(messageId, out request) ? request : null;
        }

        public IRequest GetFirstOrDefault(object response, bool includeRpc = false)
        {
            return _requests.Values.FirstOrDefault(r => r.CanSetResponse(response) && (!r.Flags.HasFlag(MessageSendingFlags.RPC) || includeRpc));
        }

        public void Remove(ulong messageId)
        {
            _requests.Remove(messageId);
        }
    }
}
