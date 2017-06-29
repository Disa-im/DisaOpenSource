using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public abstract class FileLocationBase
    {
        public UInt64 VolumeId { get; set; }

        public UInt32 LocalId { get; set; }

        public UInt64 Secret { get; set; }
    }

    public class FileLocation : FileLocationBase
    {
        public UInt32 DcId { get; set; }
    }

    public class FileLocationUnavailable : FileLocationBase
    {
    }
}
