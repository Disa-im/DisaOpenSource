using System;

namespace Disa.Framework
{
    public static class NotificationManager
    {
        public static event EventHandler<string> RemoveFromBubbleGroup;

        public static void Remove(Service service, string address)
        {
            if (RemoveFromBubbleGroup != null)
            {
                RemoveFromBubbleGroup(service, address);
            }
        }
    }
}