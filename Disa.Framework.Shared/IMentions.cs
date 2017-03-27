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
        BotCommand,
        ContextBot,
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
        /// Set the result of the <see cref="Action"/> as a <see cref="Dictionary{MentionType, char}"/>
        /// representing the tokens recognized by this service for different <see cref="MentionType"/>s.
        /// </summary>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task GetTokens(Action<Dictionary<MentionType, char>> result);

        /// <summary>
        /// Set the result of the <see cref="Action"/> as a <see cref="List{Mention}"/> of the
        /// possible <see cref="Mention"/>s for the passed in <see cref="BubbleGroup"/>.
        /// </summary>
        /// <param name="group">The group the set of mentions is for.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task GetMentions(BubbleGroup group, Action<List<Mention>> result);

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

