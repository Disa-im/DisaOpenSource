using Disa.Framework.Bots;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Stickers
{
    /// <summary>
    /// A derived <see cref="StickerPack"/> value class representing a <see cref="StickerPack"/> along with 
    /// a collection of <see cref="Sticker"/>s representing the actual <see cref="Sticker"/>s in the <see cref="StickerPack"/>
    /// and a collection of <see cref="EmojiStickerPack"/>s for the <see cref="StickerPack"/>.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class FullStickerPack : StickerPack
    {
        /// <summary>
        /// A collection of <see cref="Sticker"/>s representing this <see cref="FullStickerPack"/>.
        /// </summary>
        [ProtoMember(251)]
        public List<Sticker> Stickers { get; set; }

        /// <summary>
        /// A collection of <see cref="EmojiStickerPack"/>s representing the emojis the stickers in this <see cref="StickerPack"/>
        /// can be swapped in for.
        /// </summary>
        [ProtoMember(252)]
        public List<EmojiStickerPack> EmojiStickerPacks { get; set; }

        /// <summary>
        /// An indicator to tell if the collection of Sticker contains stickers that can be animated.
        /// 
        /// This is helpful for determining if to display a play/pause button.
        /// </summary>
        [ProtoMember(253)]
        public bool HasAnimated { get; set; }
    }
}
