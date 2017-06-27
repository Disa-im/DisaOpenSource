using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class TextBubble : VisualBubble
    {
        [ProtoMember(1)]
        public string Message {get; set; }

        [ProtoMember(2)]
        public bool HasParsedMessageForUrls { get; set; }

        public TextBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string message, string idService = null) :
            base(time, direction, address, participantAddress, party, service, null, idService)
        {
            Message = message;
        }

        public TextBubble()
        {
        }
    }
}

