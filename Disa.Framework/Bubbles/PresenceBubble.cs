using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class PresenceBubble : AbstractBubble
    {
        public enum PresenceType
        {
            Unavailable,
            Online,
            Active,
        }

        public enum PlatformType
        {
            Mobile,
            Desktop,
            Web,
            None
        }

        public PresenceType Presence { get; private set; }
        public PlatformType Platform { get; private set; }

        public bool Available
        {
            get
            {
                return IsAvailable(Presence);
            }
        }

        public static bool IsAvailable(PresenceType presence)
        {
            return presence == PresenceType.Online || presence == PresenceType.Active;
        }

        public PresenceBubble(long time, BubbleDirection direction, string address, bool party, Service service, bool available) 
            : base(time, direction, service)
        {
            Presence = available ? PresenceType.Online : PresenceType.Unavailable;
            Platform = PlatformType.None;
            Address = address;
            Party = party;
        }

        public PresenceBubble(long time, BubbleDirection direction, string address, bool party, Service service, 
            PresenceType presence, PlatformType platform) 
            : base(time, direction, service)
        {
            Presence = presence;
            Platform = platform;
            Address = address;
            Party = party;
        }

        public PresenceBubble() : base()
        {
        }
    }
}

