using System;

namespace Disa.Framework
{
    [DisaFramework]
	public interface IAnnouncement
	{
		string GetAnnouncementMessage();

		bool IsAnnouncementRecurring();

		int GetAnnouncementRecurringInterval();

		string GetAnnouncementName();

		bool HasAnnouncementExternalLink();

		string GetAnnouncementExternalLink();
	}
}
