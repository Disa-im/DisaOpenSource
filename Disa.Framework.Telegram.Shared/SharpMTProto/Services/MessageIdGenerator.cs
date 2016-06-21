// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageIdGenerator.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using SharpMTProto.Utils;

namespace SharpMTProto.Services
{
    /// <summary>
    ///     Interface for a message ID generator.
    /// </summary>
    public interface IMessageIdGenerator
    {
        ulong GetNextMessageId();
    }

    /// <summary>
    ///     The default MTProto message ID generator.
    /// </summary>
    public class MessageIdGenerator : IMessageIdGenerator
    {
        private const ulong X4Mask = ~3UL;
        private ulong _lastMessageId;

        public ulong GetNextMessageId()
        {
            // Documentation says that message id should be unixtime * 2^32.
            // But the real world calculations in other client software looking very weird.
            // Have no idea how it is actually calculated.
            ulong messageId = UnixTimeUtils.GetCurrentUnixTimestampMilliseconds();
            messageId = (messageId*4294967 + (messageId*296/1000)) & X4Mask;
            if (messageId <= _lastMessageId)
            {
                messageId = _lastMessageId + 4;
            }
            _lastMessageId = messageId;
            return messageId;
        }
    }
}
