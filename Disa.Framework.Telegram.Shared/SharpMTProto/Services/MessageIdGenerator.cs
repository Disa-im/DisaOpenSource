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
        private const ulong X4Mask = ~3UL;
        private ulong _lastMessageId;

		private ulong _timeDiffernece = 0;

		public ulong TimeDifference
		{
			get
			{
				return _timeDiffernece;
			}
			set
			{
				_timeDiffernece = value;
			}
		}

		public ulong GetNextMessageId()
        {
			// Documentation says that message id should be unixtime * 2^32.
			// But the real world calculations in other client software looking very weird.
			// Have no idea how it is actually calculated.
			//ulong messageId = UnixTimeUtils.GetCurrentUnixTimestampMilliseconds();
			ulong messageId = (UnixTimeUtils.GetCurrentUnixTimestampMilliseconds() + (TimeDifference) * 1000) * (ulong)(4294967296 / 1000f);
            while (messageId % 4 != 0)
			{
				messageId++;
			}
            _lastMessageId = messageId;
            return messageId;
        }
    }
}