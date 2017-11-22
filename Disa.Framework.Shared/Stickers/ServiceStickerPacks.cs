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
        /// The <see cref="Service.Guid"/> this collection of <see cref="StickerPack"/>s belongs to.
        /// </summary>
        [ProtoMember(151)]
        public string ServiceGuid { get; set; }

        /// <summary>
        /// A Hash representing the collection of <see cref="StickerPack"/>.
        /// 
        /// May be used in a subsequent call to determine if there were any modifications for this collection.
        /// </summary>
        [ProtoMember(152)]
        public string Hash { get; set; }

        /// <summary>
        /// The collection of <see cref="StickerPack"/>s.
        /// </summary>
        [ProtoMember(153)]
        public List<StickerPack> StickerPacks { get; set; }
    }
}
