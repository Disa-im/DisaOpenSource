// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageSendingFlags.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpMTProto.Messaging
{
    [Flags]
    public enum MessageSendingFlags
    {
        None = 0,
        Encrypted = 1,
        ContentRelated = 1 << 1,
        EncryptedAndContentRelated = Encrypted | ContentRelated,
        RPC = 2 << 1,
        EncryptedAndContentRelatedRPC = EncryptedAndContentRelated | RPC
    }
}
