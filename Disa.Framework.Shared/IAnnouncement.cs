using System;

namespace Disa.Framework
{
	public interface IAnnouncement
	{
		string GetAnnouncementMessage();

		bool IsAnnouncementRecurring();

		int GetAnnouncementRecurringInterval();

		string GetAnnouncementName();
	}
}
