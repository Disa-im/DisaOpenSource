// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IClientTransport.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharpMTProto.Transport
{
    public enum ClientTransportState
    {
        Disconnected = 0,
        Connected = 1
    }

    public interface IClientTransport : IObservable<byte[]>, IDisposable
    {
        bool IsConnected { get; }
        ClientTransportState State { get; }
        void Connect();
        Task ConnectAsync();
        Task ConnectAsync(CancellationToken token);
        void Disconnect();
        Task DisconnectAsync();
        void Send(byte[] payload);
        Task SendAsync(byte[] payload);
        Task SendAsync(byte[] payload, CancellationToken token);
    }
}
