using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
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
        /// Set the result of the <see cref="Action"/> as a <see cref="List{string}"/> of tokens to recognize
        /// for Usernames, Hashtags and BotCommands.
        /// </summary>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task GetTokens(Action<List<string>> result);

        /// <summary>
        /// Given a string representing a mentions category (usernames, hashtags, bot commands), 
        /// and and optional <see cref="BubbleGroup"/>, determine the <see cref="List{Mentions}"/>
        /// to return.
        /// </summary>
        /// <param name="token">The token representing the mentions category you are interested in.</param>
        /// <param name="group">For usernames and bot commands, the group the set of mentions is for.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task GetMentions(string token, BubbleGroup group, Action<List<Mentions>> result);

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

