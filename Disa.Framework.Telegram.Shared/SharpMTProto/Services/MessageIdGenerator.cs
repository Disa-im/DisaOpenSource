// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageIdGenerator.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using SharpMTProto.Utils;

namespace SharpMTProto.Services
{
    /// <summary>
    ///     Interface for a message ID generator.
    /// </summary>
    public interface IMessageIdGenerator
    {
		ulong TimeDifference { get; set; }

        ulong GetNextMessageId();
    }

    /// <summary>
    ///     The default MTProto message ID generator.
    /// </summary>
    public class MessageIdGenerator : IMessageIdGenerator
    {
        public ulong TimeDifference { get; set; }

        private ulong _lastMessageId;

		public ulong GetNextMessageId()
        {
            ulong messageId = (ulong)((((double)UnixTimeUtils.GetCurrentUnixTimestampMilliseconds() + ((double)TimeDifference) * 1000) * 4294967296.0) / 1000.0);
            if (messageId <= _lastMessageId)
            {
                messageId = _lastMessageId + 1;
            }
            while (messageId % 4 != 0)
			{
				messageId++;
			}
            _lastMessageId = messageId;
            return messageId;
        }
    }
}