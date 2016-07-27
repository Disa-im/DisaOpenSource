using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class ComposeBubble : AbstractBubble
    {
        [NonSerialized]
        public readonly VisualBubble BubbleToSend;
        [NonSerialized]
        public readonly Contact.ID[] Ids;

        public ComposeBubble(long time, BubbleDirection direction, Service service, 
            VisualBubble bubbleToSend, Contact.ID[] ids) 
            : base(time, direction, service)
        {
            Ids = ids;
            BubbleToSend = bubbleToSend;
        }
    }
}

