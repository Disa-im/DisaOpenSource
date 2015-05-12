// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorCode.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpMTProto
{
    [Flags]
    public enum ErrorCode : uint
    {
        /// <summary>
        ///     Message Identifier too low (most likely, client time is wrong; it would be worthwhile to synchronize it using
        ///     Message Identifier notifications and re-send the original message with the “correct” Message Identifier or wrap it
        ///     in a container with
        ///     a new Message Identifier if the original message had waited too long on the client to be transmitted).
        /// </summary>
        MsgIdIsTooSmall = 16,

        /// <summary>
        ///     Message Identifier too high (similar to the previous case, the client time has to be synchronized, and the message
        ///     re-sent with the correct Message Identifier).
        /// </summary>
        MsgIdIsTooBig = 17,

        /// <summary>
        ///     Incorrect two lower order Message Identifier bits (the server expects client message Message Identifier to be
        ///     divisible by 4).
        /// </summary>
        MsgIdBadTwoLowBytes = 18,

        /// <summary>
        ///     Container Message Identifier is the same as Message Identifier of a previously received message (this must never
        ///     happen).
        /// </summary>
        MsgIdDuplicate = 19,

        /// <summary>
        ///     Message too old, and it cannot be verified whether the server has received a message with this Message Identifier
        ///     or not.
        /// </summary>
        MsgTooOld = 20,

        /// <summary>
        ///     Message Sequence Number too low (the server has already received a message with a lower Message Identifier but with
        ///     either a higher or an equal and odd seqno).
        /// </summary>
        MsgSeqnoIsTooLow = 32,

        /// <summary>
        ///     Message Sequence Number too high (similarly, there is a message with a higher Message Identifier but with either a
        ///     lower or an equal and odd seqno).
        /// </summary>
        MsgSeqnoIsTooBig = 33,

        /// <summary>
        ///     An even Message Sequence Number expected (irrelevant message), but odd received.
        /// </summary>
        MsgSeqnoNotEven = 34,


        /// <summary>
        ///     Odd Message Sequence Number expected (relevant message), but even received.
        /// </summary>
        MsgSeqnoNotOdd = 35,

        /// <summary>
        ///     Incorrect server salt (in this case, the bad_server_salt response is received with the correct salt, and the
        ///     message is to be re-sent with it).
        /// </summary>
        IncorrectServerSalt = 48,

        /// <summary>
        ///     Invalid container.
        /// </summary>
        InvalidContainer = 64
    }
}
