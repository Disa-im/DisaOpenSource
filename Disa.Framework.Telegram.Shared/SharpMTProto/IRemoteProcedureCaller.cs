// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRemoteProcedureCaller.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SharpMTProto.Messaging;

namespace SharpMTProto
{
    /// <summary>
    ///     Interface for remote procedure calls.
    /// </summary>
    public interface IRemoteProcedureCaller
    {
        /// <summary>
        ///     Sends query without waiting for any result.
        /// </summary>
        /// <param name="requestBody">Request body.</param>
        Task SendAsync(object requestBody);

        /// <summary>
        ///     Sends RPC and wait for result response asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">Type of a result response.</typeparam>
        /// <param name="requestBody">Request body.</param>
        /// <returns>Response.</returns>
        Task<TResponse> RpcAsync<TResponse>(object requestBody);

        /// <summary>
        ///     Sets flags for message sendings.
        /// </summary>
        /// <param name="flags">Dictionary: (Type of request body)-(message sending flags).</param>
        void SetMessageSendingFlags(Dictionary<Type, MessageSendingFlags> flags);

        /// <summary>
        /// Prepares serializers for all TL-objects in assembly.
        /// </summary>
        /// <param name="assembly">Assembly.</param>
        void PrepareSerializersForAllTLObjectsInAssembly(Assembly assembly);
    }
}
