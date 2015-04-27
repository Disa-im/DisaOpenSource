using System.Collections.Generic;

namespace Disa.Framework
{
    internal class BubbleGroupCache
    {
        public string Guid { get; private set; }
        public string Name { get; private set; }
        public DisaThumbnail Photo { get; private set; }
        public List<DisaParticipant> Participants { get; private set; }

        public BubbleGroupCache(string guid, string name, DisaThumbnail photo, List<DisaParticipant> participants)
        {
            Guid = guid;
            Name = name;
            Photo = photo;
            Participants = participants;
        }
    }
}