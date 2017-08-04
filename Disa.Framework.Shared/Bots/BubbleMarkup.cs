﻿using System;
using ProtoBuf;

namespace Disa.Framework.Bots
{
    /// <summary>
    /// BubbleMarkup derived classes allow you to specify various markup attributes for a <see cref="Bubble"/>.
    /// </summary>
    [Serializable]
    [ProtoContract]
    [ProtoInclude(102, typeof(BubbleMarkupMentionUsername))]
    [ProtoInclude(103, typeof(BubbleMarkupHashtag))]
    [ProtoInclude(104, typeof(BubbleMarkupBotCommand))]
    [ProtoInclude(105, typeof(BubbleMarkupUrl))]
    [ProtoInclude(106, typeof(BubbleMarkupEmail))]
    [ProtoInclude(107, typeof(BubbleMarkupBold))]
    [ProtoInclude(108, typeof(BubbleMarkupItalic))]
    [ProtoInclude(109, typeof(BubbleMarkupCode))]
    [ProtoInclude(110, typeof(BubbleMarkupPre))]
    // 111 is removed because BubbleMarkupTextUrl now inherits BubbleMarkupUrl
    [ProtoInclude(112, typeof(BubbleMarkupMentionName))]
    [ProtoInclude(113, typeof(InputBubbleMarkupMentionName))]
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

        [ProtoMember(4)]
        public bool IsMyself { get; set; }

        /// <summary>
        /// For <see cref="BubbleMarkupPre"/>
        /// </summary>
        [ProtoMember(5)]
        public string Language { get; set; }

        /// <summary>
        /// For <see cref="BubbleMarkupTextUrl"/>
        /// 
        /// This represents a url with an alternative text representation
        /// For example: "Google" with this backing Url field set to http://google.com.
        /// </summary>
        [ProtoMember(6)]
        public string Url { get; set; }
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

    [Serializable]
    [ProtoContract]
    public class BubbleMarkupHashtag : BubbleMarkup
    {
    }

    [Serializable]
    [ProtoContract]
    public class BubbleMarkupBotCommand : BubbleMarkup
    {
    }

    [Serializable]
    [ProtoContract]
    [ProtoInclude(200, typeof(BubbleMarkupTextUrl))]
    public class BubbleMarkupUrl : BubbleMarkup
    {
        [ProtoMember(251)]
        public string Url { get; set; }
        [ProtoMember(252)]
        public string Title { get; set; }
        [ProtoMember(253)]
        public string Description { get; set; }
        [ProtoMember(254)]
        public string ImageUrl { get; set; }
        [ProtoMember(255)]
        public bool HasFetched { get; set; }
		[ProtoMember(256)]
		public string CannonicalUrl { get; set; }

        public bool IsFetching { get; set; }
        public int CrawlAttempts { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public class BubbleMarkupEmail : BubbleMarkup
    {
    }

    [Serializable]
    [ProtoContract]
    public class BubbleMarkupBold : BubbleMarkup
    {
    }

    [Serializable]
    [ProtoContract]
    public class BubbleMarkupItalic : BubbleMarkup
    {
    }

    [Serializable]
    [ProtoContract]
    public class BubbleMarkupCode : BubbleMarkup
    {
    }

    [Serializable]
    [ProtoContract]
    public class BubbleMarkupPre : BubbleMarkup
    {
    }

    [Serializable]
    [ProtoContract]
    public class BubbleMarkupTextUrl : BubbleMarkupUrl
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
