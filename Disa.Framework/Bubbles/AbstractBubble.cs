using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(200, typeof(PresenceBubble))]
    [ProtoInclude(201, typeof(DeliveredBubble))]
    [ProtoInclude(202, typeof(SubscribeBubble))]
    [ProtoInclude(203, typeof(TypingBubble))]
	[ProtoInclude(204, typeof(ReadBubble))]
    [ProtoInclude(205, typeof(DeliveredBubbleReceipt))]
    public abstract class AbstractBubble : Bubble
    {
        protected AbstractBubble(long time, BubbleDirection direction, Service service) : base(time, direction, service)
        {

        }

        protected AbstractBubble()
        {
            
        }
    }
}