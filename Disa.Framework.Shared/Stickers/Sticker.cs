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
        /// Name of the <see cref="Service"/> this <see cref="Sticker"/> comes from.
        /// </summary>
        [ProtoMember(152)]
        public string ServiceName { get; set; }

        /// <summary>
        /// The <see cref="StickerPack.Id"/> that contains this <see cref="Sticker"/>.
        /// </summary>
        [ProtoMember(153)]
        public string StickerPackId { get; set; }

        /// <summary>
        /// The <see cref="StickerPack.Location"/> that contains this <see cref="Sticker"/>.
        /// </summary>
        [ProtoMember(154)]
        public string StickerPackLocation { get; set; }

        /// <summary>
        /// On-device location of the <see cref="Sticker"/> representing a still image.
        /// </summary>
        [ProtoMember(155)]
        public string LocationStill { get; set; }

        /// <summary>
        /// On-device location of the <see cref="Sticker"/> representing an animated image.
        /// </summary>
        [ProtoMember(156)]
        public string LocationAnimated { get; set; }

        /// <summary>
        /// Optional <see cref="Sticker"/> width for still image.
        /// </summary>
        [ProtoMember(157)]
        public int WidthStill { get; set; }

        /// <summary>
        /// Optional <see cref="Sticker"/> width for animated image.
        /// </summary>
        [ProtoMember(158)]
        public int WidthAnimated { get; set; }

        /// <summary>
        /// Optional <see cref="Sticker"/> height for still image.
        /// </summary>
        [ProtoMember(159)]
        public int HeightStill { get; set; }

        /// <summary>
        /// Optional <see cref="Sticker"/> height for animated image.
        /// </summary>
        [ProtoMember(160)]
        public int HeightAnimated { get; set; }

        /// <summary>
        /// <see cref="Service"/> specific data as needed by the <see cref="Service"/> for 
        /// accessing and working with the <see cref="Sticker"/>.
        /// </summary>
        [ProtoMember(161)]
        public byte[] AdditionalData { get; set; }
    }
}
