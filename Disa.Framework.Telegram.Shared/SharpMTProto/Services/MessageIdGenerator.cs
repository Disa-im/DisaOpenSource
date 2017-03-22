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
		long TimeDifference { get; set; }

        ulong GetNextMessageId();
    }

    /// <summary>
    ///     The default MTProto message ID generator.
    /// </summary>
    public class MessageIdGenerator : IMessageIdGenerator
    {
        private long _timeDifference;
        private ulong _lastMessageId;

        public long TimeDifference
        {
            get
            {
                return _timeDifference;
            }
            set
            {
                _lastMessageId = 0;
                _timeDifference = value;
            }
        }

        private ulong GetCurrentUnixTimestampMilliseconds()
        {
            var time = UnixTimeUtils.GetCurrentUnixTimestampMilliseconds();
            var newTime = (ulong)((long)time + TimeDifference);
            return newTime;
        }

        public ulong GetNextMessageId()
        {
            var currentTime = GetCurrentUnixTimestampMilliseconds();
            ulong messageId = (ulong)(((double)currentTime * 4294967296.0) / 1000.0);
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