// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MTProtoSchemaEx.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Linq;
using SharpMTProto.Messaging;
using SharpTL;

// ReSharper disable once CheckNamespace

namespace SharpMTProto.Schema
{
    [TLObjectWithCustomSerializer(typeof (MessageSerializer))]
    public partial class Message: IEquatable<Message>
    {
        public Message()
        {
        }

        public Message(ulong msgId, uint seqno, object body)
        {
            MsgId = msgId;
            Seqno = seqno;
            Body = body;
        }

        #region Equality
        public bool Equals(Message other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            bool equals = MsgId == other.MsgId && Seqno == other.Seqno;
            if (Body is IEnumerable && other.Body is IEnumerable)
            {
                @equals &= ((IEnumerable) Body).Cast<object>().SequenceEqual(((IEnumerable) other.Body).Cast<object>());
            }
            else
            {
                @equals &= Body.Equals(other.Body);
            }
            return @equals;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            var other = obj as Message;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = MsgId.GetHashCode();
                hashCode = (hashCode*397) ^ (int) Seqno;
                hashCode = (hashCode*397) ^ (Body != null ? Body.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(Message left, Message right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Message left, Message right)
        {
            return !Equals(left, right);
        }
        #endregion
    }

    public partial interface IMessage
    {
        /// <summary>
        ///     Message Identifier is a (time-dependent) 64-bit number used uniquely to identify a message within a session. Client
        ///     message identifiers are divisible by 4, server message identifiers modulo 4 yield 1 if the message is a response to
        ///     a client message, and 3 otherwise. Client message identifiers must increase monotonically (within a single
        ///     session), the same as server message identifiers, and must approximately equal unixtime*2^32. This way, a message
        ///     identifier points to the approximate moment in time the message was created. A message is rejected over 300 seconds
        ///     after it is created or 30 seconds before it is created (this is needed to protect from replay attacks). In this
        ///     situation, it must be re-sent with a different identifier (or placed in a container with a higher identifier). The
        ///     identifier of a message container must be strictly greater than those of its nested messages.
        /// </summary>
        UInt64 MsgId { get; }

        /// <summary>
        ///     Message Sequence Number is a 32-bit number equal to twice the number of “content-related” messages (those requiring
        ///     acknowledgment, and in particular those that are not containers) created by the sender prior to this message and
        ///     subsequently incremented by one if the current message is a content-related message. A container is always
        ///     generated after its entire contents; therefore, its sequence number is greater than or equal to the sequence
        ///     numbers of the messages contained in it.
        /// </summary>
        UInt32 Seqno { get; }

        Object Body { get; }
    }

    public partial interface IRpcResult
    {
        UInt64 ReqMsgId { get; set; }

        Object Result { get; set; }
    }

    public partial interface IRpcError
    {
        UInt32 ErrorCode { get; set; }

        String ErrorMessage { get; set; }
    }

    public partial interface IBadMsgNotification
    {
        UInt64 BadMsgId { get; set; }

        UInt32 BadMsgSeqno { get; set; }

        UInt32 ErrorCode { get; set; }
    }
}
