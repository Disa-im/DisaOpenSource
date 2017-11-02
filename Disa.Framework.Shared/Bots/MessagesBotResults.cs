using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    /// <summary>
    /// Represents the result of an Inline Mode query via
    /// <see cref="IBots.SendBotInlineModeQuery(UpdateBotInlineQuery, Action{bool, MessagesBotResults})"/>.
    /// </summary>
    public class MessagesBotResults
    {
        public bool Gallery { get; set; }

        /// <summary>
        /// Unique identifier for the answered query.
        /// </summary>
        public string QueryId { get; set; }

        /// <summary>
        /// If passed, the offset that a client should send in the next query with the same text to receive more results. 
        /// 
        /// If an empty string, then there are no more results or the bot does not support pagination. 
        /// 
        /// Offset length can’t exceed 64 bytes.
        /// </summary>
        public string NextOffset { get; set; }
        /// <summary>
        /// Optional, represents the ability for a user to switch to a private mode chat with the bot in Inline Mode. 
        /// </summary>
        public InlineBotSwitchPM SwitchPm { get; set; }

        /// <summary>
        /// Colllecton of <see cref="BotInlineResultBase"/> representing the results
        /// of the query.
        /// </summary>
        public List<BotInlineResultBase> Results { get; set; }
    }
}
