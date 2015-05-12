// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SharpMTProto.Schema.MethodsImpl.Ex.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using SharpMTProto.Messaging;

namespace SharpMTProto.Schema
{
    public partial class MTProtoAsyncMethods
    {
        partial void SetupRemoteProcedureCaller(IRemoteProcedureCaller remoteProcedureCaller)
        {
            remoteProcedureCaller.PrepareSerializersForAllTLObjectsInAssembly(typeof (IMTProtoAsyncMethods).Assembly);

            var flags = new Dictionary<Type, MessageSendingFlags>
            {
                {typeof (ReqPqArgs), MessageSendingFlags.None},
                {typeof (ReqDHParamsArgs), MessageSendingFlags.None},
                {typeof (SetClientDHParamsArgs), MessageSendingFlags.None},
                {typeof (HttpWaitArgs), MessageSendingFlags.Encrypted},
            };
            remoteProcedureCaller.SetMessageSendingFlags(flags);
        }
    }
}
