// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TelegramClient.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SharpMTProto;
using SharpMTProto.Transport;
using SharpTelegram.Schema.Layer18;
using SharpMTProto.Schema;
using System.Collections.Generic;

namespace SharpTelegram
{
    public class TelegramClient : IDisposable
    {
        private readonly TelegramAppInfo _appInfo;
        private readonly IClientTransportConfig _transportConfig;
        private Config _config;
        private IMTProtoClientConnection _connection;
        private bool _isDisposed;
        private ITelegramAsyncMethods _methods;

        public TelegramClient(IClientTransportConfig transportConfig,
            ConnectionConfig connectionConfig,
            TelegramAppInfo appInfo,
            IMTProtoClientBuilder builder = null)
        {
            if (builder == null)
            {
                builder = MTProtoClientBuilder.Default;
            }

            _transportConfig = transportConfig;
            _appInfo = appInfo;
            _connection = builder.BuildConnection(_transportConfig);
            _connection.Configure(connectionConfig);
            _methods = new TelegramAsyncMethods(_connection);
        }

        public MTProtoConnectionState State
        {
            get { return _connection.State; }
        }

        public ITelegramAsyncMethods Methods
        {
            get { return _methods; }
        }

        public EventHandler<List<object>> OnUpdate
        {
            get
            {
                return _connection.OnUpdate;
            }
            set
            {
                _connection.OnUpdate = value;
            }
        }

        public EventHandler OnUpdateTooLong
        {
            get
            {
                return _connection.OnUpdateTooLong;
            }
            set
            {
                _connection.OnUpdateTooLong = value;
            }
        }

        public IMTProtoAsyncMethods ProtoMethods
        {
            get { return _connection.Methods; }
        }

        public TimeSpan DefaultRpcTimeout
        {
            get { return _connection.DefaultRpcTimeout; }
            set { _connection.DefaultRpcTimeout = value; }
        }

        public TimeSpan DefaultConnectTimeout
        {
            get { return _connection.DefaultConnectTimeout; }
            set { _connection.DefaultConnectTimeout = value; }
        }

        public bool IsConnected
        {
            get { return _connection.IsConnected; }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public async Task<MTProtoConnectResult> Connect()
        {
            ThrowIfDisposed();
            if (IsConnected)
            {
                return MTProtoConnectResult.Success;
            }
            MTProtoConnectResult result = await _connection.Connect();

            await InitConnectionAndGetConfig();

            return result;
        }

        private async Task InitConnectionAndGetConfig()
        {
            _config =
                await
                    _methods.InvokeWithLayer18Async(new InvokeWithLayer18Args
                    {
                        Query =
                            new InitConnectionArgs
                            {
                                ApiId = _appInfo.ApiId,
                                AppVersion = _appInfo.AppVersion,
                                DeviceModel = _appInfo.DeviceModel,
                                LangCode = _appInfo.LangCode,
                                SystemVersion = _appInfo.SystemVersion,
                                Query = new HelpGetConfigArgs()
                            }
                    }) as Config;
        }

        public Task Disconnect()
        {
            ThrowIfDisposed();
            return _connection.Disconnect();
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
                if (_connection != null)
                {
                    _connection.Disconnect();
                    _connection.Dispose();
                }
                _methods = null;
            }
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("Telegram client was disposed.");
            }
        }
    }
}
