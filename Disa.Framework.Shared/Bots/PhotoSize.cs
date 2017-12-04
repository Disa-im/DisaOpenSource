using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public class PhotoSize
    {
        public string Type { get; set; }

        public FileLocation Location { get; set; }

        public UInt32 W { get; set; }

        public UInt32 H { get; set; }

        public int Size { get; set; }
    }

    public class PhotoCachedSize : PhotoSize
    {
        public Byte[] Bytes { get; set; }
    }
}
