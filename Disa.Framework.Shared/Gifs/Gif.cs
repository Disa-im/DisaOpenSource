using ProtoBuf;
using System;

namespace Disa.Framework.Gifs
{
    /// <summary>
    /// A value class representing metadata for a <see cref="Gif"/>.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class Gif
    {
       /// <summary>
        /// Id of the <see cref="Gif"/>.
        /// </summary>
        [ProtoMember(151)]
        public string Id { get; set; }

        /// <summary>
        /// Name of the <see cref="Service"/> this <see cref="Gif"/> comes from.
        /// </summary>
        [ProtoMember(152)]
        public string ServiceName { get; set; }

        /// <summary>
        /// On-device location of the <see cref="Gif"/> representing a still image.
        /// </summary>
        [ProtoMember(153)]
        public string LocationStill { get; set; }

        /// <summary>
        /// On-device location of the <see cref="Gif"/> representing an animated image.
        /// </summary>
        [ProtoMember(154)]
        public string LocationAnimated { get; set; }

        /// <summary>
        /// Optional <see cref="Gif"/> width for still image.
        /// </summary>
        [ProtoMember(155)]
        public int WidthStill { get; set; }

        /// <summary>
        /// Optional <see cref="Gif"/> width for animated image.
        /// </summary>
        [ProtoMember(156)]
        public int WidthAnimated { get; set; }

        /// <summary>
        /// Optional <see cref="Gif"/> height for still image.
        /// </summary>
        [ProtoMember(157)]
        public int HeightStill { get; set; }

        /// <summary>
        /// Optional <see cref="Gif"/> height for animated image.
        /// </summary>
        [ProtoMember(158)]
        public int HeightAnimated { get; set; }

        /// <summary>
        /// <see cref="Service"/> specific data as needed by the <see cref="Gif"/> for 
        /// accessing and working with the <see cref="Gif"/>.
        /// </summary>
        [ProtoMember(159)]
        public byte[] AdditionalData { get; set; }
    }
}
