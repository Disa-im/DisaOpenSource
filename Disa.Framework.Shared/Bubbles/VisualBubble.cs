using System;
using Newtonsoft.Json;
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
    [ProtoInclude(211, typeof(SoloInformationBubble))]
    public abstract class VisualBubble : Bubble
    {
        public enum MediaType
        {
            None, Audio, Video, Text, Location, File, Contact,Image
        }

        public static string QuotedNameMyself
        {
            get
            {
                return "&^%$#@?!myself!?@#$%^&";
            }
        }

		public static string NonSignedChannel
		{
			get
			{
				return "&^%$#@?!nonSignedChannel!?@#$%^&";
			}
		}

        [ProtoMember(251)]
        public bool Deleted { get; set; }
        [ProtoMember(252)]
        public string IdService { get; set; }
        [ProtoMember(254)]
        public string IdService2 { get; set; }
        [ProtoMember(255)]
        public byte[] AdditionalData { get; set; }
        [ProtoMember(256)]
        public string QuotedAddress { get; set;} 
        [ProtoMember(257)]
        public string QuotedIdService { get; set;}
        [ProtoMember(258)]
        public string QuotedIdService2 {get; set;}
        [ProtoMember(259)]
        public MediaType QuotedType{ get; set;}
        [ProtoMember(260)]
        public string QuotedContext{get; set;}
        [ProtoMember(261)]
        public long QuotedSeconds{ get; set;}
        [ProtoMember(262)]
        private byte[] _quotedThumbnail { get; set;}
        //FIXME: Temporary hack to support plugins that are built against older framework versions.
        //       The Disa UI in framework version 30+ requires that HasQuotedThumbnail to be set true
        //       whenever QuotedThumbnail is set. Will be removed in framework version 31.
        public byte[] QuotedThumbnail
        {
            get
            {
                return _quotedThumbnail;
            }
            set
            {
                _quotedThumbnail = value;
                if (_quotedThumbnail != null && _quotedThumbnail.Length != 0)
                {
                    HasQuotedThumbnail = true;
                }
            }
        }
        [ProtoMember(263)]
        public bool HasQuotedThumbnail { get; set; }
        [ProtoMember(264)]
        public double QuotedLocationLatitude { get; set; }
        [ProtoMember(265)]
        public double QuotedLocationLongitude { get; set; }

		[JsonIgnore]
        [NonSerialized]
        public string ID;
		[JsonIgnore]
        [NonSerialized]
        public bool ContractInfo;
		[JsonIgnore]
        [NonSerialized]
        public bool NeedsPhoto = true;
		[JsonIgnore]
        [NonSerialized]
        internal BubbleGroup BubbleGroupReference;
		[JsonIgnore]
        [NonSerialized]
        public object Tag;
		[JsonIgnore]
        [NonSerialized]
        public ThumbnailTransfer QuotedThumbnailTransfer;
		[JsonIgnore]
        [NonSerialized]
        public bool QuotedThumbnailDownloadFailed;
		[NonSerialized]
		public bool InvalidatedCache;

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
