// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MTProtoClientBuilder.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using SharpMTProto.Annotations;
using SharpMTProto.Authentication;
using SharpMTProto.Messaging;
using SharpMTProto.Services;
using SharpMTProto.Transport;
using SharpTL;

// ReSharper disable MemberCanBePrivate.Global
using System;

namespace SharpMTProto
{
    public interface IMTProtoClientBuilder
    {
        [NotNull]
        IMTProtoClientConnection BuildConnection([NotNull] IClientTransportConfig clientTransportConfig);

        [NotNull]
        IAuthKeyNegotiator BuildAuthKeyNegotiator([NotNull] IClientTransportConfig clientTransportConfig);
    }

    public partial class MTProtoClientBuilder : IMTProtoClientBuilder
    {
        public static readonly IMTProtoClientBuilder Default;

        private readonly IEncryptionServices _encryptionServices;
        private readonly IHashServices _hashServices;
        private readonly IKeyChain _keyChain;
        private readonly IMessageCodec _messageCodec;
        private readonly IMessageIdGenerator _messageIdGenerator;
        private readonly INonceGenerator _nonceGenerator;
        private readonly TLRig _tlRig;
        private readonly IClientTransportFactory _clientTransportFactory;

        static MTProtoClientBuilder()
        {
            Default = CreateDefault();
        }

        public MTProtoClientBuilder(
            [NotNull] IClientTransportFactory clientTransportFactory,
            [NotNull] TLRig tlRig,
            [NotNull] IMessageIdGenerator messageIdGenerator,
            [NotNull] IMessageCodec messageCodec,
            [NotNull] IHashServices hashServices,
            [NotNull] IEncryptionServices encryptionServices,
            [NotNull] INonceGenerator nonceGenerator,
            [NotNull] IKeyChain keyChain)
        {
            _clientTransportFactory = clientTransportFactory;
            _tlRig = tlRig;
            _messageIdGenerator = messageIdGenerator;
            _messageCodec = messageCodec;
            _hashServices = hashServices;
            _encryptionServices = encryptionServices;
            _nonceGenerator = nonceGenerator;
            _keyChain = keyChain;
        }

        IMTProtoClientConnection IMTProtoClientBuilder.BuildConnection(IClientTransportConfig clientTransportConfig)
        {
            return new MTProtoClientConnection(clientTransportConfig, _clientTransportFactory, _tlRig, _messageIdGenerator, _messageCodec);
        }

        IAuthKeyNegotiator IMTProtoClientBuilder.BuildAuthKeyNegotiator(IClientTransportConfig clientTransportConfig)
        {
            return new AuthKeyNegotiator(clientTransportConfig,
                this,
                _tlRig,
                _nonceGenerator,
                _hashServices,
                _encryptionServices,
                _keyChain);
        }

        [NotNull]
        public static IMTProtoClientConnection BuildConnection([NotNull] IClientTransportConfig clientTransportConfig)
        {
            return Default.BuildConnection(clientTransportConfig);
        }

        [NotNull]
        public static IAuthKeyNegotiator BuildAuthKeyNegotiator([NotNull] IClientTransportConfig clientTransportConfig)
        {
            return Default.BuildAuthKeyNegotiator(clientTransportConfig);
        }

        private static MTProtoClientBuilder CreateDefault()
        {
            var clientTransportFactory = new ClientTransportFactory();
            var tlRig = new TLRig();
            var messageIdGenerator = new MessageIdGenerator();
            var hashServices = new HashServices();
            var encryptionServices = new EncryptionServices();
            var randomGenerator = new RandomGenerator();
            var messageCodec = new MessageCodec(tlRig, hashServices, encryptionServices, randomGenerator);
            var keyChain = new KeyChain(tlRig, hashServices);
            var nonceGenerator = new NonceGenerator();

            return new MTProtoClientBuilder(clientTransportFactory,
                tlRig,
                messageIdGenerator,
                messageCodec,
                hashServices,
                encryptionServices,
                nonceGenerator,
                keyChain);
        }
    }
}
