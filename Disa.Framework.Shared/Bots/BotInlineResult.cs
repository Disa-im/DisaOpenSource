using System;

namespace Disa.Framework.Bots
{
    public abstract class BotInlineResultBase
    {
        /// <summary>
        /// Unique identifier for this result, 1-64 Bytes.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The unique identifier for the original query that produced this result.
        /// </summary>
        public string QueryId { get; set; }

        public string Type { get; set; }

        /// <summary>
        /// Title for the result.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Short description of the result.
        /// </summary>
        public string Description { get; set; }

        public BotInlineMessage SendMessage { get; set; }
    }

    /// <summary>
    /// Represents one result of an inline mode query.
    /// </summary>
    public class BotInlineResult : BotInlineResultBase
    {
        public string ContentUrl { get; set; }

        /// <summary>
        /// A valid URL for the result.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// A valid URL for the thumbnail of the result.
        /// </summary>
        public string ThumbUrl { get; set; }

        public string ContentType { get; set; }
        
        /// <summary>
        /// Width of the result.
        /// </summary>
        public UInt32 W { get; set; }

        /// <summary>
        /// Height of the result.
        /// </summary>
        public UInt32 H { get; set; }

        /// <summary>
        /// Duration of the result.
        /// </summary>
        public UInt32 Duration { get; set; }
    }

    public class BotInlineMediaResult : BotInlineResultBase
    {
        public Photo Photo { get; set; }

        public Document Document { get; set; }
    }

}
