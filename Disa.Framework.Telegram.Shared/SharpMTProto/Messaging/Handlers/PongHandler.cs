using System;
using SharpMTProto.Messaging.Handlers;
using SharpMTProto.Schema;
using System.Threading.Tasks;
using Nito.AsyncEx;
using SharpTL;
using Disa.Framework.Telegram;

namespace SharpMTProto.Messaging.Handlers
{
    public class PongHandler : ResponseHandler<IPong>
    {
        private readonly IRequestsManager _requestsManager;

        public PongHandler(IRequestsManager requestsManager)
        {
            _requestsManager = requestsManager;
        }

        protected override Task HandleInternalAsync(IMessage responseMessage)
        {
            var pong = responseMessage.Body as Pong;

            IRequest request = _requestsManager.Get(pong.MsgId);
            if (request == null)
            {
                Console.WriteLine(string.Format("Request for response of type '{0}' not found.", responseMessage.Body.GetType()));
                return TaskConstants.Completed;
            }

            request.SetResponse(responseMessage.Body);

            return TaskConstants.Completed;
        }
    }
}