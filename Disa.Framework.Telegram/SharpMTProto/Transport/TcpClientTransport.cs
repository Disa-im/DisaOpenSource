// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TcpClientTransport.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using BigMath.Utils;
using Nito.AsyncEx;
using SharpMTProto.Utils;
using SharpTL;

namespace SharpMTProto.Transport
{
    using Annotations;

    /// <summary>
    ///     MTProto TCP clientTransport.
    /// </summary>
    public class TcpClientTransport : IClientTransport
    {
        private const int PacketLengthBytesCount = 4;
        private readonly TimeSpan _connectTimeout;
        private readonly byte[] _readerBuffer;

        private readonly AsyncLock _stateAsyncLock = new AsyncLock();
        private readonly byte[] _tempLengthBuffer = new byte[PacketLengthBytesCount];

        private CancellationTokenSource _connectionCancellationTokenSource;
        private Subject<byte[]> _in = new Subject<byte[]>();
        private int _nextPacketBytesCountLeft;
        private byte[] _nextPacketDataBuffer;
        private TLStreamer _nextPacketStreamer;
        private Task _receiverTask;
        private Socket _socket;
        private volatile ClientTransportState _state = ClientTransportState.Disconnected;
        private int _tempLengthBufferFill;
        private int _packetNumber;
        private volatile bool _isDisposed;
        private readonly IPEndPoint _remoteEndPoint;

        private readonly bool _isOnServerSide;

        private Action _onDisconnectInternally;

        public TcpClientTransport(TcpClientTransportConfig config)
        {
            if (config.Port <= 0 || config.Port > ushort.MaxValue)
            {
                throw new ArgumentException(string.Format("Port {0} is incorrect.", config.Port));
            }

            IPAddress ipAddress;
            if (!IPAddress.TryParse(config.IPAddress, out ipAddress))
            {
                throw new ArgumentException(string.Format("IP address [{0}] is incorrect.", config.IPAddress));
            }

            _remoteEndPoint = new IPEndPoint(ipAddress, config.Port);
            _connectTimeout = config.ConnectTimeout;

            _readerBuffer = new byte[config.MaxBufferSize];

            _socket = new Socket(_remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public TcpClientTransport([NotNull] Socket socket, bool isOnServerSide = true)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }

            _isOnServerSide = isOnServerSide;
            _socket = socket;
            _remoteEndPoint = _socket.RemoteEndPoint as IPEndPoint;
            _readerBuffer = new byte[_socket.ReceiveBufferSize];
            if (_socket.IsConnected())
            {
                _state = ClientTransportState.Connected;
            }
        }

        public IDisposable Subscribe(IObserver<byte[]> observer)
        {
            ThrowIfDisposed();
            return _in.Subscribe(observer);
        }

        public bool IsConnected
        {
            get { return State == ClientTransportState.Connected; }
        }

        public ClientTransportState State
        {
            get { return _state; }
        }

        public void Connect()
        {
            ThrowIfDisposed();
            ThrowIfOnServerSide();
            ConnectAsync().Wait();
        }

        public Task ConnectAsync()
        {
            return ConnectAsync(CancellationToken.None);
        }

        public async Task ConnectAsync(CancellationToken token)
        {
            ThrowIfDisposed();
            ThrowIfOnServerSide();
            using (await _stateAsyncLock.LockAsync(token))
            {
                if (State == ClientTransportState.Connected)
                {
                    return;
                }

                var args = new SocketAsyncEventArgs {RemoteEndPoint = _remoteEndPoint};
                
                var awaitable = new SocketAwaitable(args);

                try
                {
                    _packetNumber = 0;
                    await _socket.ConnectAsync(awaitable);
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    _state = ClientTransportState.Disconnected;
                    throw;
                }

                switch (args.SocketError)
                {
                    case SocketError.Success:
                    case SocketError.IsConnected:
                        _state = ClientTransportState.Connected;
                        break;
                    default:
                        _state = ClientTransportState.Disconnected;
                        break;
                }
                if (_state != ClientTransportState.Connected)
                {
                    return;
                }
                _connectionCancellationTokenSource = new CancellationTokenSource();
                _receiverTask = StartReceiver(_connectionCancellationTokenSource.Token);
            }
        }

        public void Disconnect()
        {
            DisconnectAsync().Wait();
        }

        public void RegisterOnDisconnectInternally(Action onDisconnectInternally)
        {
            _onDisconnectInternally = onDisconnectInternally;
        }

        public async Task DisconnectAsync()
        {
            ThrowIfDisposed();
            using (await _stateAsyncLock.LockAsync())
            {
                if (_state == ClientTransportState.Disconnected)
                {
                    return;
                }
                var args = new SocketAsyncEventArgs();
                var awaitable = new SocketAwaitable(args);
                try
                {
                    if (_socket.Connected)
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                        await _socket.DisconnectAsync(awaitable);
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }

                _state = ClientTransportState.Disconnected;
            }
        }

        public void Send(byte[] payload)
        {
            SendAsync(payload).Wait();
        }

        public Task SendAsync(byte[] payload)
        {
            return SendAsync(payload, CancellationToken.None);
        }

        public Task SendAsync(byte[] payload, CancellationToken token)
        {
            ThrowIfDisposed();
            return Task.Run(async () =>
            {
                var packet = new TcpTransportPacket(_packetNumber++, payload);

                var args = new SocketAsyncEventArgs();
                args.SetBuffer(packet.Data, 0, packet.Data.Length);

                var awaitable = new SocketAwaitable(args);
                await _socket.SendAsync(awaitable);
            }, token);
        }

        private Task StartReceiver(CancellationToken token)
        {
            
            return Task.Run(async () =>
            {
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(_readerBuffer, 0, _readerBuffer.Length);
                var awaitable = new SocketAwaitable(args);

                while (!token.IsCancellationRequested && _socket.IsConnected())
                {
                    try
                    {
                        if (_socket.Available == 0)
                        {
                            await Task.Delay(10, token);
                            continue;
                        }
                        await _socket.ReceiveAsync(awaitable);
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e);
                    }
                    if (args.SocketError != SocketError.Success)
                    {
                        break;
                    }
                    int bytesRead = args.BytesTransferred;
                    if (bytesRead <= 0)
                    {
                        break;
                    }

                    try
                    {
                        await ProcessReceivedDataAsync(new ArraySegment<byte>(_readerBuffer, 0, bytesRead));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Critical error while precessing received data: " + e);
                        break;
                    }
                }
                try
                {
                    await DisconnectAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                if (_onDisconnectInternally != null)
                {
                    _onDisconnectInternally();
                }
            }, token);
        }

        private async Task ProcessReceivedDataAsync(ArraySegment<byte> buffer)
        {
            try
            {
                int bytesRead = 0;
                while (bytesRead < buffer.Count)
                {
                    int startIndex = buffer.Offset + bytesRead;
                    int bytesToRead = buffer.Count - bytesRead;

                    if (_nextPacketBytesCountLeft == 0)
                    {
                        int tempLengthBytesToRead = PacketLengthBytesCount - _tempLengthBufferFill;
                        tempLengthBytesToRead = (bytesToRead < tempLengthBytesToRead) ? bytesToRead : tempLengthBytesToRead;
                        Buffer.BlockCopy(buffer.Array, startIndex, _tempLengthBuffer, _tempLengthBufferFill, tempLengthBytesToRead);

                        _tempLengthBufferFill += tempLengthBytesToRead;
                        if (_tempLengthBufferFill < PacketLengthBytesCount)
                        {
                            break;
                        }

                        startIndex += tempLengthBytesToRead;
                        bytesToRead -= tempLengthBytesToRead;

                        _tempLengthBufferFill = 0;
                        _nextPacketBytesCountLeft = _tempLengthBuffer.ToInt32();

                        if (_nextPacketDataBuffer == null || _nextPacketDataBuffer.Length < _nextPacketBytesCountLeft || _nextPacketStreamer == null)
                        {
                            _nextPacketDataBuffer = new byte[_nextPacketBytesCountLeft];
                            _nextPacketStreamer = new TLStreamer(_nextPacketDataBuffer);
                        }

                        // Writing packet length.
                        _nextPacketStreamer.Write(_tempLengthBuffer);
                        _nextPacketBytesCountLeft -= PacketLengthBytesCount;
                        bytesRead += PacketLengthBytesCount;
                    }

                    bytesToRead = bytesToRead > _nextPacketBytesCountLeft ? _nextPacketBytesCountLeft : bytesToRead;

                    _nextPacketStreamer.Write(buffer.Array, startIndex, bytesToRead);

                    bytesRead += bytesToRead;
                    _nextPacketBytesCountLeft -= bytesToRead;

                    if (_nextPacketBytesCountLeft > 0)
                    {
                        break;
                    }

                    var packet = new TcpTransportPacket(_nextPacketDataBuffer, 0, (int) _nextPacketStreamer.Position);

                    await ProcessReceivedPacket(packet);

                    _nextPacketBytesCountLeft = 0;
                    _nextPacketStreamer.Position = 0;
                }
            }
            catch (Exception)
            {
                if (_nextPacketStreamer != null)
                {
                    _nextPacketStreamer.Dispose();
                    _nextPacketStreamer = null;
                }
                _nextPacketDataBuffer = null;
                _nextPacketBytesCountLeft = 0;

                throw;
            }
        }

        private async Task ProcessReceivedPacket(TcpTransportPacket packet)
        {
            await Task.Run(() => _in.OnNext(packet.GetPayloadCopy()));
        }

        private void ThrowIfOnServerSide()
        {
            if (_isOnServerSide)
            {
                throw new NotSupportedException("Not supported in server client mode.");
            }
        }

        #region Disposing
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            if (!isDisposing)
            {
                return;
            }

            if (_connectionCancellationTokenSource != null)
            {
                _connectionCancellationTokenSource.Cancel();
                _connectionCancellationTokenSource = null;
            }
            if (_receiverTask != null)
            {
                try
                {
                    if (!_receiverTask.IsCompleted)
                    {
                        _receiverTask.Wait(1000);
                    }
                }
                catch
                {
                    // cancellation token, fall through
                }
                if (_receiverTask.IsCompleted)
                {
                    _receiverTask.Dispose();
                }
                else
                {
                    Console.WriteLine("Receiver task did not completed on transport disposing.");
                }
                _receiverTask = null;
            }
            if (_nextPacketStreamer != null)
            {
                _nextPacketStreamer.Dispose();
                _nextPacketStreamer = null;
            }
            if (_in != null)
            {
                _in.OnCompleted();
                _in.Dispose();
                _in = null;
            }
            if (_socket != null)
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Disconnect(false);
                    _socket.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    _socket = null;
                }
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
        #endregion
    }
}
