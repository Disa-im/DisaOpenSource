// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SocketAwaitable.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SharpMTProto.Utils
{
    public sealed class SocketAwaitable : INotifyCompletion
    {
        private static readonly Action Sentinel = () => { };

        private Action _continuation;
        private bool _isCompleted;

        public SocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException("eventArgs");
            }
            EventArgs = eventArgs;
            eventArgs.Completed += delegate
            {
                Action prev = _continuation ?? Interlocked.CompareExchange(ref _continuation, Sentinel, null);
                if (prev != null)
                {
                    prev();
                }
            };
        }

        public SocketAsyncEventArgs EventArgs { get; private set; }

        public bool IsCompleted
        {
            get { return _isCompleted; }
            internal set { _isCompleted = value; }
        }

        public void OnCompleted(Action continuation)
        {
            if (_continuation == Sentinel || Interlocked.CompareExchange(ref _continuation, continuation, null) == Sentinel)
            {
                Task.Run(continuation);
            }
        }

        internal void Reset()
        {
            _isCompleted = false;
            _continuation = null;
        }

        public SocketAwaitable GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
            if (EventArgs.SocketError != SocketError.Success)
            {
                throw new SocketException((int) EventArgs.SocketError);
            }
        }
    }
}
