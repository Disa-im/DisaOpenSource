using System;
using System.Collections.Generic;
using System.Text;
using Disa.Framework.Bots;
using ProtoBuf;

namespace Disa.Framework.Stickers
{
    /// <summary>
    /// A representation for a set of stickers that can be used to swap in for an emoji.
    /// 
    /// The set is identified using an emoticon (e.g., :) ) for that emoji.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class EmojiStickerPack
    {
        /// <summary>
        /// The string representation of an emoji (e.g., :) ).
        /// 
        /// This will be used to identify a suitable set of stickers that can be swapped
        /// in for that emoji.
        /// </summary>
        [ProtoMember(151)]
        public string Emoticon { get; set; }

        /// <summary>
        /// The list of <see cref="Sticker.Id"/> identifying the sticker representations.
        /// 
        /// IMPORTANT: Because the collection is serialized via ProtoBuf which has no understanding of any empty list
        /// we use an auto-property initializer to make sure we never have a null value for this list.
        /// References:
        /// https://github.com/mgravell/protobuf-net/issues/221
        /// http://geekswithblogs.net/WinAZ/archive/2015/06/30/whatrsquos-new-in-c-6.0-auto-property-initializers.aspx
        /// </summary>
        [ProtoMember(152)]
        public List<string> Stickers { get; set; } = new List<string>();
    }
}
