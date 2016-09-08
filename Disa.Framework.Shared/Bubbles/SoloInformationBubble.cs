using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class SoloInformationBubble : VisualBubble
    {
        [ProtoMember(1)]
        public string Message { get; private set; }

        [ProtoMember(2)]
        public bool RaiseNotification { get; set; }

        public SoloInformationBubble()
        {
        }

        public SoloInformationBubble(long time, BubbleDirection direction, string address, 
                                     string participantAddress, bool party, Service service, string message) :
            base(time, direction, address, participantAddress, party, service)
        {
            Message = message;
        }
    }
}

