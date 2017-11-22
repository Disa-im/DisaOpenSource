using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Stickers
{
    /// <summary>
    /// A value class representing metadata for the location of a <see cref="Sticker"/>.
    /// </summary>
    public class StickerLocationInfo
    {
        /// <summary>
        /// A <see cref="string"/> representing the location.
        /// 
        /// If <see cref="IsUrl"/> is false, then this represents an on-device location. If <see cref="IsUrl"/>
        /// is true, then this represents a remote http location.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// True if <see cref="Location"/> represents a remote http location. False if <see cref="Location"/> represents
        /// an on-device location.
        /// </summary>
        public bool IsUrl { get; set; }
    }
}
