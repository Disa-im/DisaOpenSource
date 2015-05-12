// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMTProtoClientConnection.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using SharpMTProto.Messaging;
using SharpMTProto.Schema;

namespace SharpMTProto
{
    /// <summary>
    ///     Interface of MTProto connection.
    /// </summary>
    public interface IMTProtoClientConnection : IDisposable, IRemoteProcedureCaller
    {
        /// <summary>
        ///     A state.
        /// </summary>
        MTProtoConnectionState State { get; }

        /// <summary>
        ///     Is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        ///     Default RPC timeout.
        /// </summary>
        TimeSpan DefaultRpcTimeout { get; set; }

        /// <summary>
        ///     Default connect timeout.
        /// </summary>
        TimeSpan DefaultConnectTimeout { get; set; }

        /// <summary>
        ///     Is encryption supported.
        /// </summary>
        bool IsEncryptionSupported { get; }

        /// <summary>
        ///     MTProto async methods.
        /// </summary>
        IMTProtoAsyncMethods Methods { get; }

        /// <summary>
        ///     Configure connection.
        /// </summary>
        /// <param name="config">Connection configuration.</param>
        void Configure(ConnectionConfig config);

        /// <summary>
        ///     Diconnect.
        /// </summary>
        Task Disconnect();

        /// <summary>
        ///     Connect.
        /// </summary>
        Task<MTProtoConnectResult> Connect();

        /// <summary>
        ///     Connect.
        /// </summary>
        Task<MTProtoConnectResult> Connect(CancellationToken cancellationToken);

        /// <summary>
        ///     Sends request and wait for a response asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">Type of a response.</typeparam>
        /// <param name="requestBody">Request body.</param>
        /// <param name="flags">Request message sending flags.</param>
        /// <returns>Response.</returns>
        Task<TResponse> RequestAsync<TResponse>(object requestBody, MessageSendingFlags flags);

        /// <summary>
        ///     Sends request and wait for a response asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">Type of a response.</typeparam>
        /// <param name="requestBody">Request body.</param>
        /// <param name="flags">Request message sending flags.</param>
        /// <param name="timeout">Timeout.</param>
        /// <returns>Response.</returns>
        Task<TResponse> RequestAsync<TResponse>(object requestBody, MessageSendingFlags flags, TimeSpan timeout);

        /// <summary>
        ///     Sends request and wait for a response asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">Type of a response.</typeparam>
        /// <param name="requestBody">Request body.</param>
        /// <param name="flags">Request message sending flags.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Response.</returns>
        Task<TResponse> RequestAsync<TResponse>(object requestBody, MessageSendingFlags flags, CancellationToken cancellationToken);

        /// <summary>
        ///     Sends request and wait for a response asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">Type of a response.</typeparam>
        /// <param name="requestBody">Request body.</param>
        /// <param name="flags">Request message sending flags.</param>
        /// <param name="timeout">Timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Response.</returns>
        Task<TResponse> RequestAsync<TResponse>(object requestBody, MessageSendingFlags flags, TimeSpan timeout, CancellationToken cancellationToken);

        /// <summary>
        ///     Updates salt.
        /// </summary>
        /// <param name="salt">New salt.</param>
        void UpdateSalt(ulong salt);
    }
}
