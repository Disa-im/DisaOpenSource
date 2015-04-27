using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
	[Serializable]
	[ProtoContract]
	public class ReadBubble : AbstractBubble
    {
        [ProtoMember(1)]
		public long ReadTime { get; set; }

        [ProtoMember(2)]
        public bool Updated { get; set; }

		public ReadBubble(long time, BubbleDirection direction, Service service, string address, 
            string participantAddress, long readTime, bool party, bool updated) 
			: base(time, direction, service)
		{
			Address = address;
			ReadTime = readTime;
			ParticipantAddress = participantAddress;
            Party = party;
            Updated = updated;
		}

		public ReadBubble()
		{

		}
    }
}

