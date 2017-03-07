using Disa.Framework.Bots;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public enum MentionType
    {
        Username,
        Hashtag,
        BotCommand
    }

    [DisaFramework]
    public interface IMentions
    {
        //
        // Original interface - for these you can use the following test methodology:
        //
        // // Does the service implement the required interface?
        // var newChannelUi = service as INewChannel
        // if (newChannelUi != null)
        // {
        //     .
        //     .
        //     .
        // }
        //

        /// <summary>
        /// Based on the passed in <see cref="MentionType"/>, set the result of the <see cref="Action"/> as
        /// the string to recognize for a mention category (e.g., Usernames, Hashtags BotCommands).
        /// </summary>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task GetToken(MentionType mentionType, Action<string> result);

        /// <summary>
        /// Given a string representing a mentions category (usernames, hashtags, bot commands), 
        /// and and optional <see cref="BubbleGroup"/>, determine the <see cref="List{Mentions}"/>
        /// to return.
        /// </summary>
        /// <param name="token">The token representing the mentions category you are interested in.</param>
        /// <param name="group">For usernames and bot commands, the group the set of mentions is for.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task GetMentions(string token, BubbleGroup group, Action<List<Mention>> result);

        /// <summary>
        /// Set the result of the <see cref="Action"/> as a <see cref="List{Hashtag}"/> of the most recent hashtags.
        /// </summary>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task GetRecentHashtags(Action<List<Hashtag>> result);

        /// <summary>
        /// Set the result of the <see cref="Action"/> as true if the <see cref="List{Hashtag}"/> was successfully
        /// set, false otherwise.
        /// </summary>
        /// <param name="hashtags">The <see cref="List{Hashtag}"/> of hashtags to set.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task SetRecentHashtags(List<Hashtag> hashtags, Action<bool> result);

        /// <summary>
        /// Set the result of the <see cref="Action"/> as true if the recent hashtags were cleared, false otherwise.
        /// </summary>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task ClearRecentHashtags(Action<bool> result);

        Task GetContactsByUsername(string username, Action<List<Contact>> contacts);

        // TODO
        Task GetInlineBotResults(BotContact bot, string query, string offset, Action<BotResults> botResults);

        //
        // Begin interface extensions below. For these you must use the following test methodology
        // or something similar:
        // 
        // // Do we have the required method?
        // if(DisaFrameworkMethods.Missing(service, DisaFrameWorkMethods.INewChannelXxx)
        // {
        //     return;
        // }
        //
        // // Ok to proceed
        //

    }
}

