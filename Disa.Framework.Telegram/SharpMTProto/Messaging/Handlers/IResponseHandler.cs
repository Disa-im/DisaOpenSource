// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IResponseHandler.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using SharpMTProto.Schema;

namespace SharpMTProto.Messaging.Handlers
{
    /// <summary>
    ///     Response handler.
    /// </summary>
    public interface IResponseHandler
    {
        /// <summary>
        ///     Response type.
        /// </summary>
        Type ResponseType { get; }

        /// <summary>
        ///     Handle response.
        /// </summary>
        /// <param name="responseMessage">Message with a response.</param>
        /// <returns>Completiong task.</returns>
        Task HandleAsync(IMessage responseMessage);
    }
}
