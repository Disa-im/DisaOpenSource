// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageContainerHandler.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using SharpMTProto.Schema;
using System;

namespace SharpMTProto.Messaging.Handlers
{
    public class MessageContainerHandler : ResponseHandler<IMessageContainer>
    {
        private readonly IResponseDispatcher _responseDispatcher;

        public MessageContainerHandler(IResponseDispatcher responseDispatcher)
        {
            _responseDispatcher = responseDispatcher;
        }

        protected override async Task HandleInternalAsync(IMessage responseMessage)
        {
            #region Description
            /*
             * All messages in a container must have msg_id lower than that of the container itself.
             * A container does not require an acknowledgment and may not carry other simple containers.
             * When messages are re-sent, they may be combined into a container in a different manner or sent individually.
             * 
             * Empty containers are also allowed. They are used by the server, for example,
             * to respond to an HTTP request when the timeout specified in hhtp_wait expires, and there are no messages to transmit.
             * 
             * https://core.telegram.org/mtproto/service_messages#containers
             */
            #endregion

            var msgContainer = responseMessage.Body as MsgContainer;
            if (msgContainer != null)
            {
                if (msgContainer.Messages.Any(msg => msg.MsgId >= responseMessage.MsgId || msg.Seqno > responseMessage.Seqno))
                {
                    throw new InvalidMessageException("Container MessageId must be greater than all MsgIds of inner messages.");
                }
                foreach (Message msg in msgContainer.Messages)
                {
                    await _responseDispatcher.DispatchAsync(msg);
                }
            }
            else
            {
                Console.WriteLine("Unsupported message container of type: {0}.", responseMessage.Body.GetType());
            }
        }
    }
}
