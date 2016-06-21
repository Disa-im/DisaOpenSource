// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TransportConfig.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace SharpMTProto.Transport
{
    public interface IClientTransportConfig
    {
        string TransportName { get; }
    }
}
