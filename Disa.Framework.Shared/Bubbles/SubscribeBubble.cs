using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class SubscribeBubble : AbstractBubble
    {
        [ProtoMember(1)]
        public bool Subscribe { get; private set; }

        public SubscribeBubble(long time, BubbleDirection direction, string address,
            bool party, Service service, bool subscribe)
            : base(time, direction, service)
        {
            Address = address;
            Party = party;
            Subscribe = subscribe;
        }

        public SubscribeBubble()
        {

        }
    }
}