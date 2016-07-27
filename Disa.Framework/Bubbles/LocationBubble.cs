using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class LocationBubble : VisualBubble
    {
        [ProtoMember(1)]
        public double Longitude { get; set; }

        [ProtoMember(2)]
        public double Latitude { get; set; }

        [ProtoMember(3)]
        public string Name { get; set; }

        [ProtoMember(4)]
        public byte[] Thumbnail { get; set; }

        [NonSerialized]
        public ThumbnailTransfer ThumbnailTransfer;

        [NonSerialized]
        public bool IsDownloading = false;

        [NonSerialized]
        public bool ThumbnailDownloadFailed;

        public LocationBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, double longitude, double latitude,
            string name, byte[] thumbnail, string idService = null)
            : base(time, direction, address, participantAddress, party, service, null, idService)
        {
            Longitude = longitude;
            Latitude = latitude;
            Name = name;
            Thumbnail = thumbnail;
        }

        public LocationBubble()
        {
        }
    }
}
