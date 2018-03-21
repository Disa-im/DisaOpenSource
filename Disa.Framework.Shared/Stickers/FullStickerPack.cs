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
        /// 
        /// IMPORTANT: Because the collection is serialized via ProtoBuf which has no understanding of any empty list
        /// we use an auto-property initializer to make sure we never have a null value for this list.
        /// References:
        /// https://github.com/mgravell/protobuf-net/issues/221
        /// http://geekswithblogs.net/WinAZ/archive/2015/06/30/whatrsquos-new-in-c-6.0-auto-property-initializers.aspx
        /// </summary>
        [ProtoMember(251)]
        public List<Sticker> Stickers { get; set; } = new List<Sticker>();

        /// <summary>
        /// A collection of <see cref="EmojiStickerPack"/>s representing the emojis the stickers in this <see cref="StickerPack"/>
        /// can be swapped in for.
        /// 
        /// IMPORTANT: Because the collection is serialized via ProtoBuf which has no understanding of any empty list
        /// we use an auto-property initializer to make sure we never have a null value for this list.
        /// References:
        /// https://github.com/mgravell/protobuf-net/issues/221
        /// http://geekswithblogs.net/WinAZ/archive/2015/06/30/whatrsquos-new-in-c-6.0-auto-property-initializers.aspx
        /// </summary>
        [ProtoMember(252)]
        public List<EmojiStickerPack> EmojiStickerPacks { get; set; } = new List<EmojiStickerPack>();
    }
}
