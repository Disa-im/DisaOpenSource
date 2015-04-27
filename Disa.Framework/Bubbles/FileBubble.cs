using System;
using System.IO;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class FileBubble : VisualBubble
    {
        public enum Type
        {
            Url,
            File
        };

        [ProtoMember(1)]
        public string PathNative { get; set; }

        public string Path
        {
            get
            {
                if (PathType == Type.Url)
                {
                    return PathNative;
                }

                if (File.Exists(PathNative))
                {
                    return PathNative;
                }

                if (Platform.Ready)
                {
                    try
                    {
                        return MediaManager.PatchPath(this);
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("Error patching path on FileBubble : " + ex);
                    }
                }

                return PathNative;
            }
            set
            {
                PathNative = value;
            }
        }

        [ProtoMember(2)]
        public Type PathType { get; set; }

        [ProtoMember(3)]
        public string FileName { get; set; }

        [ProtoMember(4)]
        public string MimeType { get; set; }

        [NonSerialized] 
        public BubbleTransfer Transfer;

        public FileBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string path, Type pathType, string fileName, 
            string mimeType, string idService = null) :
        base(time, direction, address, participantAddress, party, service, null, idService)
        {
            Path = path;
            PathType = pathType;
            FileName = fileName;
            MimeType = mimeType;
        }

        public FileBubble()
        {
        }
    }
}

