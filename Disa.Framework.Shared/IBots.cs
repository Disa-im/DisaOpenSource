using Disa.Framework.Bots;
using Disa.Framework.Bubbles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public interface IBots
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
        /// Set the result of the <see cref="Action"/> as a <see cref="MessagesBotCallbackAnswer"/> 
        /// for a <see cref="KeyboardButton"/> for a given <see cref="VisualBubble"/>. 
        /// </summary>
        /// <param name="group">The <see cref="BubbleGroup"/> the <see cref="VisualBubble"/> is associated with.</param>
        /// <param name="bubble">The <see cref="VisualBubble"/> the <see cref="KeyboardButton"/> is associated with.</param>
        /// <param name="button">The <see cref="KeyboardButton"/> requesting a <see cref="MessagesBotCallbackAnswer"/>.</param>
        /// <param name="answer"><see cref="Action"/> on which the <see cref="MessagesBotCallbackAnswer"/> should be set</param>
        /// <returns></returns>
        Task GetBotCallbackAnswer(BubbleGroup group, VisualBubble bubble, KeyboardButton button, Action<MessagesBotCallbackAnswer> answer);

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
