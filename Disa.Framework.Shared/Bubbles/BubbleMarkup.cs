using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(101, typeof(BubbleMarkupMention))]
    [ProtoInclude(102, typeof(BubbleMarkupMentionName))]
    [ProtoInclude(103, typeof(InputBubbleMarkupMentionName))]
    public abstract class BubbleMarkup
    {
        [ProtoMember(1)]
        public int Offset { get; set; }

        [ProtoMember(2)]
        public int Length { get; set; }

        /// <summary>
        /// For a <see cref="UsernameMention"/> mention, holds the <see cref="DisaParticipant.Address"/> for this username.
        /// 
        /// Used by <see cref="BubbleMarkupMentionName"/> and <see cref="InputBubbleMarkupMentionName"/> when a mention is
        /// made for a user without a username.
        /// </summary>
        [ProtoMember(3)]
        public string Address { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public class BubbleMarkupMention : BubbleMarkup
    {
    }

    [Serializable]
    [ProtoContract]
    public class BubbleMarkupMentionName : BubbleMarkup
    {
    }

    [Serializable]
    [ProtoContract]
    public class InputBubbleMarkupMentionName : BubbleMarkup
    {
    }
}
