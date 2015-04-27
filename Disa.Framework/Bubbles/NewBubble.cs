using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class NewBubble : VisualBubble
    {
        public NewBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service) :
            base(time, direction, address, participantAddress, party, service)
        {
        }

        public NewBubble()
        {
        }
    }
}