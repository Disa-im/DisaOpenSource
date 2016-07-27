using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class NewDayBubble : VisualBubble
    {
        [NonSerialized] public bool Used;

        public NewDayBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service) :
            base(time, direction, address, participantAddress, party, service)
        {
        }

        public NewDayBubble()
        {
        }
    }
}