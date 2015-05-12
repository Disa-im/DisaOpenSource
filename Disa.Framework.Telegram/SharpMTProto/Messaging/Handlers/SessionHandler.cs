// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SessionHandler.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Nito.AsyncEx;
using SharpMTProto.Schema;

namespace SharpMTProto.Messaging.Handlers
{
    public class SessionHandler : ResponseHandler<INewSession>
    {
        protected override Task HandleInternalAsync(IMessage responseMessage)
        {
            // TODO: implement.
            return TaskConstants.Completed;
        }
    }
}
