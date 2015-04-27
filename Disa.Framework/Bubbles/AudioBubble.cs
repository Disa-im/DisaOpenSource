using System;
using System.IO;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    public class AudioBubble : VisualBubble
    {
        public enum Type
        {
            Url,
            File
        };

        [ProtoMember(1)]
        public string AudioPathNative { get; set; }

        public string AudioPath
        {
            get
            {
                if (AudioType == Type.Url)
                {
                    return AudioPathNative;
                }

                if (File.Exists(AudioPathNative))
                {
                    return AudioPathNative;
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

                return AudioPathNative;
            }
            set
            {
                AudioPathNative = value;
            }
        }

        [ProtoMember(2)]
        public Type AudioType { get; set; }

        [ProtoMember(3)]
        public bool Recording { get; set; }

        [ProtoMember(4)]
        public int Seconds { get; set; }

		[ProtoMember(5)]
		public int DownloadAttempt { get; set; }

        [ProtoMember(6)]
        public string FileName { get; set; }

        [NonSerialized]
        public BubbleTransfer Transfer;
		[NonSerialized]
		public bool ControlPause = true;
		[NonSerialized]
		public int ControlPosition;
		[NonSerialized]
		public Action ControlPositionUpdated;
		[NonSerialized]
		public Action ControlStopped;
		[NonSerialized]
		public int ControlDuration;


        public AudioBubble(long time, BubbleDirection direction, string address,
            string participantAddress, bool party, Service service, string audioPath, Type audioType, bool recording, int seconds, string idService = null) :
            base(time, direction, address, participantAddress, party, service, null, idService)
        {
            AudioPath = audioPath;
            AudioType = audioType;
            Recording = recording;
            Seconds = seconds;
        }

        public AudioBubble()
        {
        }

		public bool CanAutoDownload()
		{           
			return DownloadAttempt < 10;
		}
    }
}