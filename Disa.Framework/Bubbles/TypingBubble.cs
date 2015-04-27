using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class TypingBubble : AbstractBubble
    {
        [ProtoMember(1)]
        public bool Typing {get; private set;}
		[ProtoMember(2)]
		public bool IsAudio {get; private set;}

        public TypingBubble(long time, BubbleDirection direction, string address, 
			bool party, Service service, bool typing, bool isAudio) :
            base(time, direction, service)
        {
            Address = address;
            Party = party;
            Typing = typing;
			IsAudio = isAudio;
        }


        public TypingBubble(long time, BubbleDirection direction, string address, string participantAddress,
            bool party, Service service, bool typing, bool isAudio) :
            base(time, direction, service)
        {
            Address = address;
            ParticipantAddress = participantAddress;
            Party = party;
            Typing = typing;
            IsAudio = isAudio;
        }

        public TypingBubble()
        {
            
        }
    }
}

