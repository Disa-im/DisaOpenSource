using ProtoBuf;
using System;
using System.Collections.Generic;


namespace Disa.Framework.Stickers
{
    /// <summary>
    /// Represents a collection of  of <see cref="StickerPack"/> for a <see cref="Service"/>.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class ServiceStickerPacks
    {
        /// <summary>
        /// The <see cref="Service.Information.ServiceName"/> this collection of <see cref="StickerPack"/>s belongs to.
        /// </summary>
        [ProtoMember(151)]
        public string ServiceName { get; set; }

        /// <summary>
        /// A Hash representing the collection of <see cref="StickerPack"/>.
        /// 
        /// May be used in a subsequent call to determine if there were any modifications for this collection.
        /// </summary>
        [ProtoMember(152)]
        public string Hash { get; set; }

        /// <summary>
        /// The collection of <see cref="StickerPack"/>s.
        /// 
        /// IMPORTANT: Because the collection is serialized via ProtoBuf which has no understanding of any empty list
        /// we use an auto-property initializer to make sure we never have a null value for this list.
        /// References:
        /// https://github.com/mgravell/protobuf-net/issues/221
        /// http://geekswithblogs.net/WinAZ/archive/2015/06/30/whatrsquos-new-in-c-6.0-auto-property-initializers.aspx
        /// </summary>
        [ProtoMember(153)]
        public List<StickerPack> StickerPacks { get; set; } = new List<StickerPack>();
    }
}
