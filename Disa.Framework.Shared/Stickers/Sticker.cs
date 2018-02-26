using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Stickers
{
    /// <summary>
    /// A value class representing metadata for a <see cref="Sticker"/>.
    /// 
    /// IMPORTANT IMPLEMENTATION NOTES:
    /// 1. Service providers must provide Sticker with Id and StickerPackId valid for a file name of:
    /// <see cref="Sticker.StickerPackId"/>-<see cref="Sticker.Id"/>
    /// Example: 435-897 
    /// 2. The combination of <see cref="Sticker.StickerPackId"/> and <see cref="Sticker.Id"/> must be unique for the service provider.
    /// 3. A retrieval of a still version of the sticker must always be available. An animated version is optional.
    /// 4. <see cref="Sticker.HasAnimated"/> will signal that retrieval of the sticker will produce both a still and animated version.
    /// 5. The fields <see cref="Sticker.LocationStill"/> and <see cref="Sticker.LocationAnimated"/> are not to be filled in
    /// by the service provider, but will be filled in by the framework.
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
        /// On-device location of the <see cref="Sticker"/> representing a still image.
        /// </summary>
        [ProtoMember(154)]
        public string LocationStill { get; set; }

        /// <summary>
        /// On-device location of the <see cref="Sticker"/> representing an animated image.
        /// </summary>
        [ProtoMember(155)]
        public string LocationAnimated { get; set; }

        /// <summary>
        /// Optional <see cref="Sticker"/> width for still image.
        /// </summary>
        [ProtoMember(156)]
        public int WidthStill { get; set; }

        /// <summary>
        /// Optional <see cref="Sticker"/> width for animated image.
        /// </summary>
        [ProtoMember(157)]
        public int WidthAnimated { get; set; }

        /// <summary>
        /// Optional <see cref="Sticker"/> height for still image.
        /// </summary>
        [ProtoMember(158)]
        public int HeightStill { get; set; }

        /// <summary>
        /// Optional <see cref="Sticker"/> height for animated image.
        /// </summary>
        [ProtoMember(159)]
        public int HeightAnimated { get; set; }

        /// <summary>
        /// <see cref="Service"/> specific data as needed by the <see cref="Service"/> for 
        /// accessing and working with the <see cref="Sticker"/>.
        /// </summary>
        [ProtoMember(160)]
        public byte[] AdditionalData { get; set; }
    }
}
