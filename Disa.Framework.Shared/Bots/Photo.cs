using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public class Photo
    {
        public System.UInt64 Id { get; set; }

        public System.UInt64 AccessHash { get; set; }

        public System.UInt32 Date { get; set; }

        public List<PhotoSize> Sizes { get; set; }

        public byte[] AdditionalData { get; set; }
    }
}
