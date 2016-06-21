// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SocketExtensions.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Net.Sockets;

namespace SharpMTProto.Utils
{
    public static class SocketExtensions
    {
        public static SocketAwaitable AcceptAsync(this Socket socket, SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.AcceptAsync(awaitable.EventArgs))
            {
                awaitable.IsCompleted = true;
            }
            return awaitable;
        }

        public static SocketAwaitable ConnectAsync(this Socket socket, SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.ConnectAsync(awaitable.EventArgs))
            {
                awaitable.IsCompleted = true;
            }
            return awaitable;
        }

        public static SocketAwaitable DisconnectAsync(this Socket socket, SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.DisconnectAsync(awaitable.EventArgs))
            {
                awaitable.IsCompleted = true;
            }
            return awaitable;
        }

        public static SocketAwaitable ReceiveAsync(this Socket socket, SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.ReceiveAsync(awaitable.EventArgs))
            {
                awaitable.IsCompleted = true;
            }
            return awaitable;
        }

        public static SocketAwaitable SendAsync(this Socket socket, SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.SendAsync(awaitable.EventArgs))
            {
                awaitable.IsCompleted = true;
            }
            return awaitable;
        }

        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
