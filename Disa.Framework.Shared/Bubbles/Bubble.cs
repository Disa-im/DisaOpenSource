using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(100, typeof(VisualBubble))]
    [ProtoInclude(101, typeof(AbstractBubble))]
    public abstract class Bubble
    {
        public enum BubbleDirection
        {
            Outgoing,
            Incoming
        };

        public enum BubbleStatus
        {
            Sent,
            Delivered,
            Waiting,
            Failed,
        };

        [ProtoMember(150)]
        public long Time { get; set; }

        [ProtoMember(151)]
        public BubbleDirection Direction { get; set; }

        [ProtoMember(152)]
        public string Address { get; set; }

        [ProtoMember(153)]
        public string ParticipantAddress { get; set; }

        [ProtoMember(154)]
        public bool Party { get; set; }

        [ProtoMember(155)]
        public BubbleStatus Status { get; set; }

        [ProtoMember(156)]
        public string ParticipantAddressNickname { get; set; }

        [ProtoMember(157)]
        public bool ExtendedParty { get; set;}

        [NonSerialized]
        public Service Service;

        protected Bubble(long time, BubbleDirection direction, Service service)
        {
            Status = BubbleStatus.Waiting;
            Time = time;
            Direction = direction;
            Service = service;
        }

        protected Bubble()
        {
        }
    }
}

