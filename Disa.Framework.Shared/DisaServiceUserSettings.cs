using System;

namespace Disa.Framework
{
    [Serializable]
    public class DisaServiceUserSettings
    {
        public int NotificationLed { get; set; }
        public string Ringtone { get; set; }
        public bool BlockNotifications { get; set; }
        public int ServiceColor { get; set; }
        public string VibrateOption { get; set; }
        public string VibrateOptionCustomPattern { get; set; }
        public int SentBubbleColor { get; set; }
        public int ReceivedBubbleColor { get; set; }
        public int SentFontColor { get; set; }
        public int ReceivedFontColor { get; set; }
        public bool BubbleColorsChosen { get; set; }


        public DisaServiceUserSettings(int notificationLed, string ringtone, bool blockNotifications, int serviceColor,
                                       string vibratePattern, string vibrateOptionsCustomPattern, int sentBubbleColor, int receivedBubbleColor, int sentFontColor, int receivedFontColor, bool bubbleColorsChosen)
        {
            NotificationLed = notificationLed;
            Ringtone = ringtone;
            BlockNotifications = blockNotifications;
            ServiceColor = serviceColor;
            VibrateOption = vibratePattern;
            VibrateOptionCustomPattern = vibrateOptionsCustomPattern;
            SentBubbleColor = sentBubbleColor;
            ReceivedBubbleColor = receivedBubbleColor;
            SentFontColor = sentFontColor;
            ReceivedFontColor = receivedFontColor;
            BubbleColorsChosen = bubbleColorsChosen;
        }

        public DisaServiceUserSettings()
        {
        }

        public static DisaServiceUserSettings Default
        {
            get
            {
                return new DisaServiceUserSettings(DefaultNotificationLedColor, null, false, DefaultServiceColor, null, null, 0, 0, 0, 0, false);
            }
        }

        public static int DefaultServiceColor
        {
            get
            {
                return 0x0097a7;
            }
        }

		public static int DefaultNotificationLedColor
		{
			get
			{
				return 0xffffff;
			}
		}
    }
}
