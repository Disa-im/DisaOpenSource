using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class PartyInformationBubble : VisualBubble
    {
        [ProtoMember(1)]
        public string Message { get; private set; }

        public PartyInformationBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string message) :
            base(time, direction, address, participantAddress, party, service)
        {
            Message = message;
        }

        public PartyInformationBubble()
        {
        }
    }
}