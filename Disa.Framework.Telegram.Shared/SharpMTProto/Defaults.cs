// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Defaults.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpMTProto
{
    public static class Defaults
    {
        public static readonly TimeSpan RpcTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
    }
}
