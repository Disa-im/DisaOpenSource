// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Request.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using SharpMTProto.Schema;

namespace SharpMTProto.Messaging
{
    public interface IRequest
    {
        IMessage Message { get; }
        MessageSendingFlags Flags { get; }
        bool IsAcknowledged { get; }

        /// <summary>
        ///     Acknowledge UTC date time.
        /// </summary>
        DateTime? AcknowledgeTime { get; }

        /// <summary>
        ///     Response UTC date time.
        /// </summary>
        DateTime? ResponseTime { get; }

        void Acknowledge();

        void SetResponse(object response);

        bool CanSetResponse(object response);
        Task SendAsync();
        void SetException(Exception ex);
    }

    public class Request<TResponse> : IRequest
    {
        private readonly CancellationToken _cancellationToken;
        private readonly Func<IRequest, CancellationToken, Task> _sendAsync;
        private readonly TaskCompletionSource<TResponse> _taskCompletionSource = new TaskCompletionSource<TResponse>();

        public Request(IMessage message, MessageSendingFlags flags, Func<IRequest, CancellationToken, Task> sendAsync, CancellationToken cancellationToken)
        {
            _sendAsync = sendAsync;
            _cancellationToken = cancellationToken;
            Message = message;
            Flags = flags;
            cancellationToken.Register(() => _taskCompletionSource.TrySetCanceled());
        }

        public DateTime? ResponseTime { get; private set; }

        public IMessage Message { get; private set; }

        public MessageSendingFlags Flags { get; private set; }

        public bool IsAcknowledged { get; private set; }

        /// <summary>
        ///     Acknowledge UTC date time.
        /// </summary>
        public DateTime? AcknowledgeTime { get; private set; }

        public void Acknowledge()
        {
            if (IsAcknowledged)
            {
                return;
            }
            IsAcknowledged = true;
            AcknowledgeTime = DateTime.UtcNow;
        }

        public void SetResponse(object response)
        {
            if (!CanSetResponse(response))
            {
                throw new MTProtoException(string.Format("Wrong response type {0}. Expected: {1}.", response.GetType(), typeof (TResponse).Name));
            }

            Acknowledge();
            ResponseTime = DateTime.UtcNow;
            _taskCompletionSource.TrySetResult((TResponse) response);
        }

        public bool CanSetResponse(object response)
        {
            return response is TResponse;
        }

        public Task SendAsync()
        {
            return _sendAsync(this, _cancellationToken);
        }

        public void SetException(Exception ex)
        {
            Acknowledge();
            ResponseTime = DateTime.UtcNow;

            _taskCompletionSource.TrySetException(ex);
        }

        public Task<TResponse> GetResponseAsync()
        {
            return _taskCompletionSource.Task;
        }
    }
}
