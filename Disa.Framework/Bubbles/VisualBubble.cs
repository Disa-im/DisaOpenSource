using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(200, typeof(TextBubble))]
    [ProtoInclude(201, typeof(NewBubble))]
    [ProtoInclude(202, typeof(ImageBubble))]
    [ProtoInclude(203, typeof(VideoBubble))]
    [ProtoInclude(204, typeof(AudioBubble))]
    [ProtoInclude(205, typeof(NewDayBubble))]
    [ProtoInclude(206, typeof(LocationBubble))]
    [ProtoInclude(207, typeof(PartyInformationBubble))]
    [ProtoInclude(208, typeof(FileBubble))]
    [ProtoInclude(209, typeof(StickerBubble))]
    [ProtoInclude(210, typeof(ContactBubble))]
    public abstract class VisualBubble : Bubble
    {
        [ProtoMember(251)]
        public bool Deleted { get; set; }
        [ProtoMember(252)]
        public string IdService { get; set; }
        [ProtoMember(254)]
        public string IdService2 { get; set; }
        [ProtoMember(255)]
        public byte[] AdditionalData { get; set; }

        [NonSerialized]
        public string ID;
        [NonSerialized]
        public bool ContractInfo;
        [NonSerialized]
        public bool NotNowTime;
        [NonSerialized]
        public bool NeedsPhoto = true;
        [NonSerialized]
        internal BubbleGroup BubbleGroupReference;
        [NonSerialized]
        public object Tag;

        protected VisualBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string id = null, string idService = null)
            : base(time, direction, service)
        {
            Address = address;
            ParticipantAddress = participantAddress;
            Party = party;

            IdService = idService;
            if (id == null)
            {
                ID = Guid.NewGuid().ToString();
            }
            else
            {
                ID = id;
            }
        }

        protected VisualBubble()
        {
        }
    }
}