using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class DeliveredBubbleReceipt : AbstractBubble
    {
        [ProtoMember(1)]
        public VisualBubble BubbleUpdated { get; set; }

        public DeliveredBubbleReceipt(long time, BubbleDirection direction, Service service, VisualBubble bubbleUpdated)
            : base(time, direction, service)
        {
            BubbleUpdated = bubbleUpdated;
        }

        public DeliveredBubbleReceipt()
        {
        }
    }
}

