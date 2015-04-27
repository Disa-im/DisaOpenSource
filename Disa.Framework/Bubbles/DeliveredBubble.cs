using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
	//TODO: support for participants (parties)
    [Serializable]
    [ProtoContract]
    public class DeliveredBubble : AbstractBubble
    {
        [ProtoMember(1)]
        public string VisualBubbleID { get; set; }

		public DeliveredBubble(long time, BubbleDirection direction, Service service, string address, string visualBubbleID)
			 : base(time, direction, service)
        {
			Address = address;
            VisualBubbleID = visualBubbleID;
        }

        public DeliveredBubble()
        {
            
        }
    }
}