using System;
using System.IO;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class ImageBubble : VisualBubble
    {
        public enum Type
        {
            Url,
            File
        };

        [ProtoMember(1)]
        public string ImagePathNative { get; set; }

        public string ImagePath
        {
            get
            {
                if (ImageType == Type.Url)
                {
                    return ImagePathNative;
                }

                if (File.Exists(ImagePathNative))
                {
                    return ImagePathNative;
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

                return ImagePathNative;
            }
            set
            {
                ImagePathNative = value;
            }
        }

        [ProtoMember(2)]
        public Type ImageType { get; set; }

        [ProtoMember(3)]
        public byte[] ThumbnailBytes { get; set; }

        [ProtoMember(4)]
        public int DownloadAttempt { get; set; }

        [ProtoMember(5)]
        public bool IsAnimated { get; set; }

        [ProtoMember(6)]
        public string FileName { get; set; }

        [ProtoMember(7)]
        public bool NullThumbnail { get; set; }

        [ProtoMember(8)]
        public int Width { get; set; }

        [ProtoMember(9)]
        public int Height { get; set; }

        [NonSerialized] 
        public BubbleTransfer Transfer;

        [NonSerialized]
        public ThumbnailTransfer ThumbnailTransfer;

        [NonSerialized]
        public bool ThumbnailDownloadFailed;

        public ImageBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string imagePath, Type imageType, byte[] thumbnailBytes, string idService = null,
            bool isAnimated = false) :
            base(time, direction, address, participantAddress, party, service, null, idService)
        {
            ImagePath = imagePath;
            ImageType = imageType;
            ThumbnailBytes = thumbnailBytes;
            IsAnimated = isAnimated;
        }

        public ImageBubble()
        {
        }

        public bool CanAutoDownload()
        {           
            return DownloadAttempt < 10;
        }
    }
}
