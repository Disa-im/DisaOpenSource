using SQLite;

namespace Disa.Framework
{
    internal class BubbleGroupSettings
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Guid { get; set; }

        public bool Mute { get; set; }

        public bool Unread { get; set; }

        public long LastUnreadSetTime { get; set; }

        public byte[] ReadTimes { get; set; }

        [Ignore]
        public DisaReadTime[] ReadTimesCached { get; set; }
        [Ignore]
        public bool ReadTimesCachedSet { get; set; }
    }
}