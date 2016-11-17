using System;

namespace Disa.Framework
{
	public interface IAnnouncement
	{
		string GetAnnouncementMessage();

		bool IsAnnouncementReoccuring();

		int GetAnnouncementRecurringInterval();

		string GetAnnouncementName();
	}
}
