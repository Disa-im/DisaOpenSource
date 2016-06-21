// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IClientTransportFactory.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace SharpMTProto.Transport
{
    /// <summary>
    ///     Interface for a client transport factory. Allows to create new client transports.
    /// </summary>
    public interface IClientTransportFactory
    {
        /// <summary>
        ///     Creates a new client TCP transport.
        /// </summary>
        /// <param name="clientTransportConfig">Transport info.</param>
        /// <returns>Cllient TCP transport.</returns>
        IClientTransport CreateTransport(IClientTransportConfig clientTransportConfig);
    }
}
