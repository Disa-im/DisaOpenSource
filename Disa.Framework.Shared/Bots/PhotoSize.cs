using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public abstract class PhotoSizeBase
    {
        string Type { get; set; }
    }

    public class PhotoSize : PhotoSizeBase
    {
        FileLocationBase Location { get; set; }

        UInt32 W { get; set; }

        UInt32 H { get; set; }

        UInt32 Size { get; set; }
    }

    public class PhotoSizeEmpty : PhotoSizeBase
    {
    }

    public class PhotoCachedSize : PhotoSize
    {
        Byte[] Bytes { get; set; }
    }
}
