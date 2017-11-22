using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Stickers
{
    /// <summary>
    /// A value class representing metadata for a <see cref="Sticker"/>.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class Sticker
    {
        /// <summary>
        /// Id of the <see cref="Sticker"/>.
        /// </summary>
        [ProtoMember(151)]
        public string Id { get; set; }

        /// <summary>
        /// Guid of the <see cref="Service"/> this <see cref="Sticker"/> comes from.
        /// </summary>
        [ProtoMember(152)]
        public string ServiceGuid { get; set; }

        /// <summary>
        /// The <see cref="StickerPack.Id"/> that contains this <see cref="Sticker"/>.
        /// </summary>
        public string StickerPackId { get; set; }

        /// <summary>
        /// On-device location of the <see cref="Sticker"/>.
        /// </summary>
        [ProtoMember(153)]
        public string Location { get; set; }

        /// <summary>
        /// <see cref="Service"/> specific data as needed by the <see cref="Service"/> for 
        /// accessing and working with the <see cref="Sticker"/>.
        /// </summary>
        [ProtoMember(154)]
        public byte[] AdditionalData { get; set; }
    }
}
