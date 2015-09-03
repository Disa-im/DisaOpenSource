using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class PartyInformationBubble : VisualBubble
    {
        public enum InformationType { ParticipantAdded, ParticipantRemoved, TitleChanged, ThumbnailChanged, }

        [ProtoMember(1)]
        public string Message { get; private set; }

        [ProtoMember(2)]
        public InformationType Type { get; private set; }

        [ProtoMember(3)]
        public string Influencer { get; private set; }

        [ProtoMember(4)]
        public string Affected { get; private set; }

        [ProtoMember(5)]
        public string NewTitle { get; private set; }

        public PartyInformationBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string message) :
            base(time, direction, address, participantAddress, party, service)
        {
            Message = message;
        }

        public PartyInformationBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string idService,
            InformationType type, string influencer, string affected)
            : base(time, direction, address, participantAddress, party, service, null, idService)
        {
            Influencer = influencer;
            Affected = affected;
            Type = type;
        }

        public static PartyInformationBubble CreateParticipantAdded(long time, string address,
            Service service, string idService, string participantWhomAdded, string participantAdded)
        {
            return new PartyInformationBubble(time, BubbleDirection.Incoming, address, null, true, 
                service, idService, InformationType.ParticipantAdded, participantWhomAdded, participantAdded);
        }

        public static PartyInformationBubble CreateParticipantRemoved(long time, string address,
            Service service, string idService, string participantWhomRemoved, string participantRemoved)
        {
            return new PartyInformationBubble(time, BubbleDirection.Incoming, address, null, true, 
                service, idService, InformationType.ParticipantRemoved, participantWhomRemoved, participantRemoved);
        }

        public static PartyInformationBubble CreateTitleChanged(long time, string address,
            Service service, string idService, string participantWhomChangedTitle, string newTitle)
        {
            return new PartyInformationBubble(time, BubbleDirection.Incoming, address, null, true, 
                service, idService, InformationType.TitleChanged, participantWhomChangedTitle, null)
            {
                NewTitle = newTitle,
            };
        }

        public static PartyInformationBubble CreateThumbnailChanged(long time, string address,
            Service service, string idService, string participantWhomChangedThumbnail)
        {
            return new PartyInformationBubble(time, BubbleDirection.Incoming, address, null, true, 
                service, idService, InformationType.ThumbnailChanged, participantWhomChangedThumbnail, null);
        }

        public PartyInformationBubble()
        {
        }
    }
}