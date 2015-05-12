// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnixTimeUtils.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpMTProto.Utils
{
    public static class UnixTimeUtils
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static ulong GetCurrentUnixTimestampMilliseconds()
        {
            return DateTime.UtcNow.ToUnixTimestampMilliseconds();
        }

        public static ulong ToUnixTimestampMilliseconds(this DateTime utcDateTime)
        {
            return (ulong) (utcDateTime - UnixEpoch).TotalMilliseconds;
        }

        public static DateTime DateTimeFromUnixTimestampMilliseconds(ulong milliseconds)
        {
            return UnixEpoch.AddMilliseconds(milliseconds);
        }

        public static ulong GetCurrentUnixTimestampSeconds()
        {
            return DateTime.UtcNow.ToUnixTimestampSeconds();
        }

        public static ulong ToUnixTimestampSeconds(this DateTime utcDateTime)
        {
            return (ulong) (utcDateTime - UnixEpoch).TotalSeconds;
        }

        public static DateTime DateTimeFromUnixTimestampSeconds(ulong seconds)
        {
            return UnixEpoch.AddSeconds(seconds);
        }
    }
}
