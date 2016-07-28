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

        public DisaServiceUserSettings(int notificationLed, string ringtone, bool blockNotifications, int serviceColor, 
            string vibratePattern, string vibrateOptionsCustomPattern)
        {
            NotificationLed = notificationLed;
            Ringtone = ringtone;
            BlockNotifications = blockNotifications;
            ServiceColor = serviceColor;
            VibrateOption = vibratePattern;
            VibrateOptionCustomPattern = vibrateOptionsCustomPattern;
        }

        public DisaServiceUserSettings()
        {
        }

        public static DisaServiceUserSettings Default
        {
            get
            {
                return new DisaServiceUserSettings(DefaultNotificationLedColor, null, false, DefaultServiceColor, null, null);
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

