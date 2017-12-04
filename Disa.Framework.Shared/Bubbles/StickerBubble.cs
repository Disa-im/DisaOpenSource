using System;
using System.IO;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class StickerBubble : VisualBubble
    {
        public enum Type
        {
            Url,
            File
        };

        [ProtoMember(1)]
        public string StickerPathNative { get; set; }

        public string StickerPath
        {
            get
            {
                if (StickerType == Type.Url)
                {
                    return StickerPathNative;
                }

                if (File.Exists(StickerPathNative))
                {
                    return StickerPathNative;
                }

                if (Platform.Ready)
                {
                    try
                    {
                        return MediaManager.PatchPath(this);
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("Error patching path on ImageBubble : " + ex);
                    }
                }

                return StickerPathNative;
            }
            set
            {
                StickerPathNative = value;
            }
        }

        [ProtoMember(2)]
        public Type StickerType { get; set; }

        // Proto member 4 (DownloadAttempt) deprecated

        [ProtoMember(5)]
        public string StickerId { get; set; }

        [ProtoMember(6)]
        public int Width { get; set; }

        [ProtoMember(7)]
        public int Height { get; set; }

        [ProtoMember(8)]
        public string StaticImage { get; set; }

        [ProtoMember(10)]
        public string AnimatedImage { get; set; }

        [ProtoMember(11)]
        public string AlternativeEmoji { get; set; }

		[NonSerialized]
		public BubbleTransfer Transfer;

		[NonSerialized]
		public bool DownloadFailed;

        public StickerBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string stickerId, Type stickerType, 
            int width, int height, string staticImage, string animatedImage, string idService = null) :
        base(time, direction, address, participantAddress, party, service, null, idService)
        {
            StickerType = stickerType;
            StickerId = stickerId;
            Width = width;
            Height = height;
            StaticImage = staticImage;
            AnimatedImage = animatedImage;
        }

        public StickerBubble()
        {
        }
    }
}