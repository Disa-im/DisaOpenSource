using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    /// <summary>
    /// This object represents an Inline Mode query. 
    /// 
    /// When the user sends an empty query, a bot could return some default or trending results.
    /// </summary>
    public class UpdateBotInlineQuery
    {
        /// <summary>
        /// Bot you are sending query to.
        /// </summary>
        public BotContact Bot { get; set; }

        /// <summary>
        /// Text of the query (up to 512 characters).
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Optional. Sender location, only for bots that request user location
        /// </summary>
        public GeoPoint Geo { get; set; }

        /// <summary>
        /// Offset of the results to be returned, can be controlled by the bot.
        /// </summary>
        public string Offset { get; set; }
    }

    /// <summary>
    /// Represents a result of an inline query that was chosen by the user and sent to their chat partner.
    /// </summary>
    public class UpdateBotInlineSend
    {
        /// <summary>
        /// The user that chose the result.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// The query that was used to obtain the result.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Optional. Sender location, only for bots that require user location.
        /// </summary>
        public GeoPoint Geo { get; set; }

        /// <summary>
        /// The unique identifier for the result that was chosen.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Optional. Identifier of the sent inline message. 
        /// 
        /// Available only if there is an inline keyboard attached to the message. 
        /// Will be also received in callback queries and can be used to edit the message.
        /// </summary>
        public InputBotInlineMessageID MsgId { get; set; }
    }

    public class UpdateBotCallbackQuery
    {
        public int QueryId { get; set; }

        public int UserId { get; set; }

        // TODO
        // public IPeer Peer { get; set; }

        public int MsgId { get; set; }

        public byte[] Data { get; set; }
    }

    public class UpdateInlineBotCallbackQuery
    {
        public int QueryId { get; set; }

        public int UserId { get; set; }

        public InputBotInlineMessageID MsgId { get; set; }

        public byte[] Data { get; set; }
    }
}
