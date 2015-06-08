// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MTProtoClientConnection.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BigMath.Utils;
using SharpMTProto.Annotations;
using SharpMTProto.Messaging;
using SharpMTProto.Messaging.Handlers;
using SharpMTProto.Schema;
using SharpMTProto.Services;
using SharpMTProto.Transport;
using SharpTL;
using AsyncLock = Nito.AsyncEx.AsyncLock;

namespace SharpMTProto
{
    // ReSharper disable ClassWithVirtualMembersNeverInherited.Global

    /// <summary>
    ///     MTProto connection state.
    /// </summary>
    public enum MTProtoConnectionState
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2
    }

    /// <summary>
    ///     MTProto connect result.
    /// </summary>
    public enum MTProtoConnectResult
    {
        Success,
        Timeout,
        Other
    }

    /// <summary>
    ///     MTProto connection.
    /// </summary>
    public class MTProtoClientConnection : IMTProtoClientConnection
    {
        private static readonly Random Rnd = new Random();

        private readonly AsyncLock _connectionLock = new AsyncLock();
        private readonly IMessageCodec _messageCodec;
        private readonly TLRig _tlRig;
        private readonly IMessageIdGenerator _messageIdGenerator;
        private readonly RequestsManager _requestsManager = new RequestsManager();
        private readonly IResponseDispatcher _responseDispatcher = new ResponseDispatcher();

        private readonly IClientTransport _clientTransport;
        private ConnectionConfig _config = new ConnectionConfig(null, 0);
        private CancellationToken _connectionCancellationToken;
        private CancellationTokenSource _connectionCts;
        private bool _isDisposed;
        private uint _messageSeqNumber;

        private readonly Dictionary<Type, MessageSendingFlags> _messageSendingFlags = new Dictionary<Type, MessageSendingFlags>();

        private volatile MTProtoConnectionState _state = MTProtoConnectionState.Disconnected;
        private readonly MTProtoAsyncMethods _methods;

        private readonly UpdatesHandler _updatesHandler;

        public MTProtoClientConnection(
            [NotNull] IClientTransportConfig clientTransportConfig,
            [NotNull] IClientTransportFactory clientTransportFactory,
            [NotNull] TLRig tlRig,
            [NotNull] IMessageIdGenerator messageIdGenerator,
            [NotNull] IMessageCodec messageCodec)
        {
            _tlRig = tlRig;
            _messageIdGenerator = messageIdGenerator;
            _messageCodec = messageCodec;

            DefaultRpcTimeout = Defaults.RpcTimeout;
            DefaultConnectTimeout = Defaults.ConnectTimeout;

            _methods = new MTProtoAsyncMethods(this);

            _updatesHandler = new UpdatesHandler(_tlRig);

            InitResponseDispatcher(_responseDispatcher);

            // Init transport.
            _clientTransport = clientTransportFactory.CreateTransport(clientTransportConfig);

            // Connector in/out.
            _clientTransport.ObserveOn(DefaultScheduler.Instance).Do(bytes => LogMessageInOut(bytes, "IN")).Subscribe(ProcessIncomingMessageBytes);
        
            _clientTransport.RegisterOnDisconnectInternally(() =>
            {
                Console.WriteLine("Client has been closed internally.");

                if (_state == MTProtoConnectionState.Disconnected)
                    
                {
                    return;
                }
                _state = MTProtoConnectionState.Disconnected;

                if (_connectionCts != null)
                {
                    _connectionCts.Cancel();
                    _connectionCts.Dispose();
                    _connectionCts = null;
                }
            });
        }

        public TimeSpan DefaultRpcTimeout { get; set; }
        public TimeSpan DefaultConnectTimeout { get; set; }

        public IMTProtoAsyncMethods Methods
        {
            get { return _methods; }
        }

        public MTProtoConnectionState State
        {
            get { return _state; }
        }

        public bool IsConnected
        {
            get { return _state == MTProtoConnectionState.Connected; }
        }

        public bool IsEncryptionSupported
        {
            get { return _config.AuthKey != null; }
        }

        public EventHandler<List<object>> OnUpdate
        {
            get
            {
                return _updatesHandler.OnUpdate;
            }
            set
            {
                _updatesHandler.OnUpdate = value;
            }
        }

        public EventHandler OnUpdateTooLong
        {
            get
            {
                return _updatesHandler.OnUpdateTooLong;
            }
            set
            {
                _updatesHandler.OnUpdateTooLong = value;
            }
        }

        public EventHandler<SharpMTProto.Messaging.Handlers.UpdatesHandler.State> OnUpdateState
        {
            get
            {
                return _updatesHandler.OnUpdateState;
            }
            set
            {
                _updatesHandler.OnUpdateState = value;
            }
        }

        /// <summary>
        ///     Start sender and receiver tasks.
        /// </summary>
        public async Task<MTProtoConnectResult> Connect()
        {
            return await Connect(CancellationToken.None);
        }

        /// <summary>
        ///     Connect.
        /// </summary>
        public Task<MTProtoConnectResult> Connect(CancellationToken cancellationToken)
        {
            return Task.Run(
                async () =>
                {
                    var result = MTProtoConnectResult.Other;

                    using (await _connectionLock.LockAsync(cancellationToken))
                    {
                        if (_state == MTProtoConnectionState.Connected)
                        {
                            result = MTProtoConnectResult.Success;
                            return result;
                        }
                        Debug.Assert(_state == MTProtoConnectionState.Disconnected);
                        try
                        {
                            _state = MTProtoConnectionState.Connecting;
                            Console.WriteLine("Connecting...");

                            await
                                _clientTransport.ConnectAsync(cancellationToken).ToObservable().Timeout(DefaultConnectTimeout);

                            _connectionCts = new CancellationTokenSource();
                            _connectionCancellationToken = _connectionCts.Token;

                            Console.WriteLine("Connected.");
                            result = MTProtoConnectResult.Success;
                        }
                        catch (TimeoutException)
                        {
                            result = MTProtoConnectResult.Timeout;
                            Console.WriteLine(
                                string.Format(
                                    "Failed to connect due to timeout ({0}s).",
                                    DefaultConnectTimeout.TotalSeconds));
                        }
                        catch (Exception e)
                        {
                            result = MTProtoConnectResult.Other;
                            Console.WriteLine("Failed to connect.: " + e);
                        }
                        finally
                        {
                            switch (result)
                            {
                                case MTProtoConnectResult.Success:
                                    _state = MTProtoConnectionState.Connected;
                                    break;
                                case MTProtoConnectResult.Timeout:
                                case MTProtoConnectResult.Other:
                                    _state = MTProtoConnectionState.Disconnected;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                    return result;
                },
                cancellationToken);
        }

        public Task Disconnect()
        {
            return Task.Run(
                async () =>
                {
                    using (await _connectionLock.LockAsync(CancellationToken.None))
                    {
                        if (_state == MTProtoConnectionState.Disconnected)
                        {
                            return;
                        }
                        _state = MTProtoConnectionState.Disconnected;

                        if (_connectionCts != null)
                        {
                            _connectionCts.Cancel();
                            _connectionCts.Dispose();
                            _connectionCts = null;
                        }

                        await _clientTransport.DisconnectAsync();
                    }
                },
                CancellationToken.None);
        }

        /// <summary>
        ///     Set config.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">The <paramref name="config" /> is <c>null</c>.</exception>
        public void Configure([NotNull] ConnectionConfig config)
        {
            _config = config;
            if (_config.SessionId == 0)
            {
                _config.SessionId = GetNextSessionId();
            }
        }

        public void UpdateSalt(ulong salt)
        {
            _config.Salt = salt;
        }

        public Task<TResponse> RequestAsync<TResponse>(object requestBody, MessageSendingFlags flags)
        {
            return RequestAsync<TResponse>(requestBody, flags, DefaultRpcTimeout, CancellationToken.None);
        }

        public Task<TResponse> RequestAsync<TResponse>(object requestBody, MessageSendingFlags flags, TimeSpan timeout)
        {
            return RequestAsync<TResponse>(requestBody, flags, timeout, CancellationToken.None);
        }

        public Task<TResponse> RequestAsync<TResponse>(object requestBody, MessageSendingFlags flags, CancellationToken cancellationToken)
        {
            return RequestAsync<TResponse>(requestBody, flags, DefaultRpcTimeout, cancellationToken);
        }

        public async Task<TResponse> RequestAsync<TResponse>(object requestBody, MessageSendingFlags flags, TimeSpan timeout, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDiconnected();

            var timeoutCancellationTokenSource = new CancellationTokenSource(timeout);
            using (
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCancellationTokenSource.Token,
                    cancellationToken,
                    _connectionCancellationToken))
            {
                Request<TResponse> request = CreateRequest<TResponse>(requestBody, flags, cts.Token);
                Console.WriteLine("Sending request ({0}) '{1}'.", flags, requestBody);
                await request.SendAsync();
                return await request.GetResponseAsync();
            }
        }

        public Task<TResponse> RpcAsync<TResponse>(object requestBody)
        {
            return RequestAsync<TResponse>(requestBody, GetMessageSendingFlags(requestBody));
        }

        public Task SendAsync(object requestBody)
        {
            return SendAsync(requestBody, GetMessageSendingFlags(requestBody), DefaultRpcTimeout, CancellationToken.None);
        }

        public void SetMessageSendingFlags(Dictionary<Type, MessageSendingFlags> flags)
        {
            foreach (var flag in flags)
            {
                _messageSendingFlags.Add(flag.Key, flag.Value);
            }
        }

        public Task SendAsync(object requestBody, MessageSendingFlags flags)
        {
            return SendAsync(requestBody, flags, DefaultRpcTimeout, CancellationToken.None);
        }

        public Task SendAsync(object requestBody, MessageSendingFlags flags, TimeSpan timeout)
        {
            return SendAsync(requestBody, flags, timeout, CancellationToken.None);
        }

        public Task SendAsync(object requestBody, MessageSendingFlags flags, CancellationToken cancellationToken)
        {
            return SendAsync(requestBody, flags, DefaultRpcTimeout, cancellationToken);
        }

        public async Task SendAsync(object requestBody, MessageSendingFlags flags, TimeSpan timeout, CancellationToken cancellationToken)
        {
            byte[] messageBytes = EncodeMessage(CreateMessage(requestBody, flags.HasFlag(MessageSendingFlags.ContentRelated)), flags.HasFlag(MessageSendingFlags.Encrypted));
            
            var timeoutCancellationTokenSource = new CancellationTokenSource(timeout);
            using (
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCancellationTokenSource.Token,
                    cancellationToken,
                    _connectionCancellationToken))
            {
                await SendAsync(messageBytes, cts.Token);
            }
        }

        public void PrepareSerializersForAllTLObjectsInAssembly(Assembly assembly)
        {
            _tlRig.PrepareSerializersForAllTLObjectsInAssembly(assembly);
        }

        private void InitResponseDispatcher(IResponseDispatcher responseDispatcher)
        {
            responseDispatcher.GenericHandler = new GenericRequestResponseHandler(_requestsManager);
            responseDispatcher.AddHandler(new BadMsgNotificationHandler(this, _requestsManager));
            responseDispatcher.AddHandler(new MessageContainerHandler(_responseDispatcher));
            responseDispatcher.AddHandler(new RpcResultHandler(_requestsManager, _tlRig));
            responseDispatcher.AddHandler(new SessionHandler());
            responseDispatcher.AddHandler(_updatesHandler);
        }

        private Task SendRequestAsync(IRequest request, CancellationToken cancellationToken)
        {
            return Task.Run(
                async () =>
                {
                    byte[] messageBytes = EncodeMessage(request.Message, request.Flags.HasFlag(MessageSendingFlags.Encrypted));
                    await SendAsync(messageBytes, cancellationToken);
                },
                cancellationToken);
        }

        private Request<TResponse> CreateRequest<TResponse>(object body, MessageSendingFlags flags, CancellationToken cancellationToken)
        {
            var request = new Request<TResponse>(
                CreateMessage(body, flags.HasFlag(MessageSendingFlags.ContentRelated)),
                flags,
                SendRequestAsync,
                cancellationToken);
            _requestsManager.Add(request);
            return request;
        }

        private Message CreateMessage(object body, bool isContentRelated)
        {
            return new Message(GetNextMsgId(), GetNextMsgSeqno(isContentRelated), body);
        }

        private Task SendAsync(byte[] data, CancellationToken cancellationToken)
        {
            ThrowIfDiconnected();
            LogMessageInOut(data, "OUT");
            return _clientTransport.SendAsync(data, cancellationToken);
        }

        /// <summary>
        ///     Processes incoming message bytes.
        /// </summary>
        /// <param name="messageBytes">Incoming bytes.</param>
        private void ProcessIncomingMessageBytes(byte[] messageBytes)
        {
            ThrowIfDisposed();

            try
            {
                Console.WriteLine("Processing incoming message.");
                ulong authKeyId;
                using (var streamer = new TLStreamer(messageBytes))
                {
                    if (messageBytes.Length == 4)
                    {
                        int error = streamer.ReadInt32();
                        Console.WriteLine("Received error code: {0}.", error);
                        return;
                    }
                    if (messageBytes.Length < 20)
                    {
                        throw new InvalidMessageException(
                            string.Format(
                                "Invalid message length: {0} bytes. Expected to be at least 20 bytes for message or 4 bytes for error code.",
                                messageBytes.Length));
                    }
                    authKeyId = streamer.ReadUInt64();
                }

                IMessage message;

                if (authKeyId == 0)
                {
                    // Assume the message bytes has a plain (unencrypted) message.
                    Console.WriteLine(string.Format("Auth key ID = 0x{0:X16}. Assume this is a plain (unencrypted) message.", authKeyId));

                    message = _messageCodec.DecodePlainMessage(messageBytes);

                    if (!IsIncomingMessageIdValid(message.MsgId))
                    {
                        throw new InvalidMessageException(string.Format("Message ID = 0x{0:X16} is invalid.", message.MsgId));
                    }
                }
                else
                {
                    // Assume the stream has an encrypted message.
                    Console.WriteLine(string.Format("Auth key ID = 0x{0:X16}. Assume this is encrypted message.", authKeyId));
                    if (!IsEncryptionSupported)
                    {
                        Console.WriteLine("Encryption is not supported by this connection.");
                        return;
                    }

                    ulong salt, sessionId;
                    message = _messageCodec.DecodeEncryptedMessage(messageBytes, _config.AuthKey, Sender.Server, out salt, out sessionId);
                    // TODO: check salt.
                    if (sessionId != _config.SessionId)
                    {
                        throw new InvalidMessageException(string.Format("Invalid session ID {0}. Expected {1}.", sessionId, _config.SessionId));
                    }
                    Console.WriteLine(string.Format("Received encrypted message. Message ID = 0x{0:X16}.", message.MsgId));
                }
                ProcessIncomingMessage(message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to receive a message: " + e);
            }
        }

        private async void ProcessIncomingMessage(IMessage message)
        {
            ThrowIfDisposed();

            try
            {
                Console.WriteLine("Incoming message data of type = {0}.", message.Body.GetType());

                await _responseDispatcher.DispatchAsync(message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while processing incoming message: " + e);
            }
        }

        private uint GetNextMsgSeqno(bool isContentRelated)
        {
            uint x = (isContentRelated ? 1u : 0);
            uint result = _messageSeqNumber*2 + x;
            _messageSeqNumber += x;
            return result;
        }

        private static ulong GetNextSessionId()
        {
            return ((ulong) Rnd.Next() << 32) + (ulong) Rnd.Next();
        }

        private ulong GetNextMsgId()
        {
            return _messageIdGenerator.GetNextMessageId();
        }

        private MessageSendingFlags GetMessageSendingFlags(
            object requestBody,
            MessageSendingFlags defaultSendingFlags = MessageSendingFlags.EncryptedAndContentRelatedRPC)
        {
            MessageSendingFlags flags;
            Type requestBodyType = requestBody.GetType();

            if (!_messageSendingFlags.TryGetValue(requestBodyType, out flags))
            {
                flags = defaultSendingFlags;
            }
            return flags;
        }

        private static void LogMessageInOut(byte[] messageBytes, string inOrOut)
        {
            Console.WriteLine(string.Format("{0} ({1} bytes): {2}", inOrOut, messageBytes.Length, messageBytes.ToHexString()));
        }

        private bool IsIncomingMessageIdValid(ulong messageId)
        {
            // TODO: check.
            return true;
        }

        private byte[] EncodeMessage(IMessage message, bool isEncrypted)
        {
            if (isEncrypted)
            {
                ThrowIfEncryptionIsNotSupported();
            }

            byte[] messageBytes = isEncrypted
                ? _messageCodec.EncodeEncryptedMessage(message, _config.AuthKey, _config.Salt, _config.SessionId, Sender.Client)
                : _messageCodec.EncodePlainMessage(message);

            return messageBytes;
        }

        [DebuggerStepThrough]
        private void ThrowIfDiconnected()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not allowed when disconnected.");
            }
        }

        [DebuggerStepThrough]
        private void ThrowIfEncryptionIsNotSupported()
        {
            if (!IsEncryptionSupported)
            {
                throw new InvalidOperationException("Encryption is not supported. Setup encryption first by calling Configure() method.");
            }
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("Connection was disposed.");
            }
        }
        
        #region Disposable
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            if (isDisposing)
            {
                Disconnect().Wait(5000);
                _clientTransport.Dispose();
            }
        }
        #endregion
    }
}
