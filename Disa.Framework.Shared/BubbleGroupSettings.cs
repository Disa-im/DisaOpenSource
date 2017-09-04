using System.Collections.Generic;
using SQLite;

namespace Disa.Framework
{
    internal class BubbleGroupSettings
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Guid { get; set; }

        public bool Mute { get; set; }

        public int NotificationLed { get; set; }

        public string VibrateOption { get; set; }

        public string VibrateOptionCustomPattern { get; set; }

        public string Ringtone { get; set; }

        public bool Unread { get; set; }

        public bool UnreadOffline { get; set; }

		public string UnreadIndicatorGuid { get; set; }

        public long LastUnreadSetTime { get; set; }

        public byte[] ParticipantNicknames { get; set; }

        public bool RingtoneDisabled { get; set; }

        public bool VibrateOptionDisabled { get; set; }

        public int SentBubbleColor { get; set; }

        public int ReceivedBubbleColor { get; set; }

        public int SentFontColor { get; set; }

        public int ReceivedFontColor { get; set; }

        public bool BubbleColorsChosen { get; set; }

		public bool BackgroundChosen { get; set; }

		public int BackgroundColor { get; set; }

		public string BackgroundImagePath { get; set; }

        [Ignore]
        public DisaParticipantNickname[] ParticipantNicknamesCached { get; set; }
        [Ignore]
        public bool ParticipantNicknamesCachedSet { get; set; }

        public byte[] ReadTimes { get; set; }

        [Ignore]
        public DisaReadTime[] ReadTimesCached { get; set; }
        [Ignore]
        public bool ReadTimesCachedSet { get; set; }

        public byte[] QuotedTitles { get; set; }

        [Ignore]
        public DisaQuotedTitle[] QuotedTitlesCached { get; set; }
        [Ignore]
        public bool QuotedTitlesCachedSet { get; set; }
    }
}
