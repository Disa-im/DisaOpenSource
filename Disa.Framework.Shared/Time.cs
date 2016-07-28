using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Disa.Framework
{
    public static class Time
    {
        public static ITimeFormatter CustomFormatter { get; set; }

        public static long GetUnixTimestamp(DateTime value)
        {
            var span = (value - Epoch);
            return (long)span.TotalSeconds;
        }

        public static long GetNowUnixTimestamp()
        {
            return GetUnixTimestamp(DateTime.UtcNow);
        }

        public static long GetUnixTimestampMilliseconds(DateTime value)
        {
            var span = (value - Epoch);
            return (long)span.TotalMilliseconds;
        }

        public static long GetNowUnixTimestampMilliseconds()
        {
            return GetUnixTimestampMilliseconds(DateTime.UtcNow);
        }

        public static string GetDayDisplayTime(long unixTime)
        {
            if (CustomFormatter != null)
            {
                return CustomFormatter.GetDayDisplayTime(unixTime);
            }

            var utcTime = Epoch.AddSeconds(Convert.ToDouble(unixTime));
            var localTime = utcTime.ToLocalTime();

            return localTime.ToString("D", CultureInfo.CurrentCulture);
        }

        public static string GetAbsoluteDisplayTime(long unixTime)
        {
            if (CustomFormatter != null)
            {
                return CustomFormatter.GetAbsoluteDisplayTime(unixTime);
            }

            var utcTime = Epoch.AddSeconds(Convert.ToDouble(unixTime));
            var localTime = utcTime.ToLocalTime();

            return localTime.ToString("g", CultureInfo.CurrentCulture);
        }

        public static DateTime GetLocalTime(long unixTime)
        {
            var utcTime = Epoch.AddSeconds(Convert.ToDouble(unixTime));
            return utcTime.ToLocalTime();
        }

        public static string GetLastSeenDisplayTime(long unixTime)
        {
            if (CustomFormatter != null)
            {
                return CustomFormatter.GetLastSeenDisplayTime(unixTime);
            }

            var utcTime = Epoch.AddSeconds(Convert.ToDouble(unixTime));
            var localTime = utcTime.ToLocalTime();

            var isYesterday = DateTime.Today - localTime.Date == TimeSpan.FromDays(1);
            var isToday = DateTime.Today == localTime.Date;

            if (isYesterday)
            {
                return "Last seen yesterday at " + localTime.ToString("t", CultureInfo.CurrentCulture);
            }
            if (isToday)
            {
                return "Last seen today at " + localTime.ToString("t", CultureInfo.CurrentCulture);
            }

            return "Last seen " + localTime.ToString("g", CultureInfo.CurrentCulture);
        }

        public static string GetBubbleDisplayTime(long unixTime, bool lowercase = false, bool absoluteTime = false)
        {
            if (CustomFormatter != null)
            {
                return CustomFormatter.GetBubbleDisplayTime(unixTime, lowercase, absoluteTime);
            }

            var utcTime = Epoch.AddSeconds(Convert.ToDouble(unixTime));
            var localTime = utcTime.ToLocalTime();

            return localTime.ToString("t", CultureInfo.CurrentCulture);
        }

        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0,
            DateTimeKind.Utc);
    }
}