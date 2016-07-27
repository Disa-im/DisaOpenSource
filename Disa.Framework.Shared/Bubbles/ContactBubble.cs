using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class ContactBubble : VisualBubble
    {
        [ProtoMember(1)]
        public string Name {get; set;}

        [ProtoMember(2)]
        public byte[] VCardData {get; set;}

        public ContactBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string name, byte[] vCardData, string idService = null) : 
        base(time, direction, address, participantAddress, party, service, null, idService)
        {
            Name = name;
            VCardData = vCardData;
        }

        public ContactBubble()
        {
        }
    }
}

