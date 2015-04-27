using System;
using System.IO;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class VideoBubble : VisualBubble
    {
        public enum Type
        {
            Url,
            File
        };

        [ProtoMember(1)]
        public string VideoPathNative { get; set; }

        public string VideoPath
        {
            get
            {
                if (VideoType == Type.Url)
                {
                    return VideoPathNative;
                }

                if (File.Exists(VideoPathNative))
                {
                    return VideoPathNative;
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

                return VideoPathNative;
            }
            set
            {
                VideoPathNative = value;
            }
        }

        [ProtoMember(2)]
        public Type VideoType { get; set; }

        [ProtoMember(3)]
        public byte[] ThumbnailBytes { get; set; }

        [ProtoMember(4)]
        public string FileName { get; set; }

        [NonSerialized]
        public BubbleTransfer Transfer;

        public VideoBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string videoPath, Type videoType, byte[] thumbnailBytes, string idService = null) :
            base(time, direction, address, participantAddress, party, service, null, idService)
        {
            VideoPath = videoPath;
            VideoType = videoType;
            ThumbnailBytes = thumbnailBytes;
        }


        public VideoBubble()
        {
            
        }
    }
}
