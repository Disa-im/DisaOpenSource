using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public abstract class DocumentAttributeBase
    {
    }

    public class DocumentAttributeImageSize : DocumentAttributeBase
    {
        public UInt32 W { get; set; }

        public UInt32 H { get; set; }
    }

    public class DocumentAttributeAnimated : DocumentAttributeBase
    {
    }

    public class DocumentAttributeSticker : DocumentAttributeBase
    {
        public string Alt { get; set; }

        public InputStickerSetBase Stickerset { get; set; }
    }

    public class DocumentAttributeVideo : DocumentAttributeBase
    {
        public UInt32 Duration { get; set; }

        public UInt32 W { get; set; }

        public UInt32 H { get; set; }
    }

    public class DocumentAttributeAudio : DocumentAttributeBase
    {
        public bool Voice { get; set; }

        public UInt32 Duration { get; set; }

        public string Title { get; set; }

        public string Performer { get; set; }

        public Byte[] Waveform { get; set; }
    }

    public class DocumentAttributeFilename : DocumentAttributeBase
    {
        public string FileName { get; set; }
    }
}
