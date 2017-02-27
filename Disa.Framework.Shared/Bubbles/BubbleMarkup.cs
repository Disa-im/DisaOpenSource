using System;
using ProtoBuf;

namespace Disa.Framework.Bubbles
{
    /// <summary>
    /// BubbleMarkup derived classes allow you to specify various markup attributes for a <see cref="Bubble"/>.
    /// </summary>
    [Serializable]
    [ProtoContract]
    [ProtoInclude(101, typeof(BubbleMarkupMentionUsername))]
    [ProtoInclude(102, typeof(BubbleMarkupMentionName))]
    [ProtoInclude(103, typeof(InputBubbleMarkupMentionName))]
    public abstract class BubbleMarkup
    {
        /// <summary>
        /// The offset that the markup will start at.
        /// </summary>
        [ProtoMember(1)]
        public int Offset { get; set; }

        /// <summary>
        /// The length of the markup.
        /// </summary>
        [ProtoMember(2)]
        public int Length { get; set; }

        /// <summary>
        /// For <see cref="BubbleMarkupMentionName"/> and <see cref="InputBubbleMarkupMentionName"/>, holds the 
        /// <see cref="DisaParticipant.Address"/>.
        /// </summary>
        [ProtoMember(3)]
        public string Address { get; set; }
    }

    /// <summary>
    /// Holds markup attributes for the mention of a username in a <see cref="Bubble"/>.
    /// 
    /// Example: @Bill
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class BubbleMarkupMentionUsername : BubbleMarkup
    {
    }

    /// <summary>
    /// Holds markup attributes for the mention of a name in a <see cref="Bubble"/>.
    /// 
    /// Example: Bill
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class BubbleMarkupMentionName : BubbleMarkup
    {

    }

    /// <summary>
    /// Holds markup attributes for the mention of a name when constructing a message
    /// to send out.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class InputBubbleMarkupMentionName : BubbleMarkup
    {
    }
}
