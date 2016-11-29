using System;
namespace Disa.Framework.Telegram
{
	public partial class Telegram : IAnnouncement
	{
		public string GetAnnouncementMessage()
		{
			return "Welcome to the Telegram alpha plugin! Please note that we currently" +
				" do not support secret conversations or starting channels." + Environment.NewLine + Environment.NewLine +
              	"We are working hard on completing this functionality before the beta release.";
		}

		public string GetAnnouncementName()
		{
			return "telegraminitialalpha1";
		}

		public int GetAnnouncementRecurringInterval()
		{
			throw new NotImplementedException();
		}

		public bool IsAnnouncementRecurring()
		{
			return false;
		}

		public string GetAnnouncementExternalLink()
		{
			throw new NotImplementedException();
		}

		public bool HasAnnouncementExternalLink()
		{
			return false;
		}
	}
}
