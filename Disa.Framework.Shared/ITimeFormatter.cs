namespace Disa.Framework
{
    [DisaFramework]
    public interface ITimeFormatter
    {
        string GetDayDisplayTime(long unixTime);
        string GetAbsoluteDisplayTime(long unixTime);
        string GetLastSeenDisplayTime(long unixTime);
        string GetBubbleDisplayTime(long unixTime, bool lowercase = false, bool absoluteTime = false);
    }
}