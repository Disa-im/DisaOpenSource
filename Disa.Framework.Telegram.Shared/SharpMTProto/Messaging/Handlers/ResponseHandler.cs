// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResponseHandler.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using SharpMTProto.Schema;

namespace SharpMTProto.Messaging.Handlers
{
    public abstract class ResponseHandler<TResponse> : IResponseHandler where TResponse : class
    {
        private static readonly Type ResponseTypeInternal = typeof (TResponse);

        public virtual Type ResponseType
        {
            get { return ResponseTypeInternal; }
        }

        public Task HandleAsync(IMessage responseMessage)
        {
            var response = responseMessage.Body as TResponse;
            if (response == null)
            {
                throw new MTProtoException(string.Format("Expected response type to be '{0}', but found '{1}'.", ResponseTypeInternal, responseMessage.Body.GetType()));
            }
            return HandleInternalAsync(responseMessage);
        }

        protected abstract Task HandleInternalAsync(IMessage responseMessage);
    }
}
