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

        /// <summary>
        /// Set the result of the <see cref="Action{bool, MessagesBotResults}"/> as a bool indicating
        /// success and a <see cref="MessagesBotResults"/> as the result of the query.
        /// </summary>
        /// <param name="query">A <see cref="UpdateBotInlineQuery"/> defining the Inline Mode query to the bot.</param>
        /// <param name="results">The <see cref="Action{bool, MessagesBotResults}"/> on which success of the request and the
        /// <see cref="MessagesBotResults"/> result of the query.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{bool, MessagesBotResults}"/>.</returns>
        Task SendBotInlineModeQuery(BubbleGroup group, UpdateBotInlineQuery query, Action<MessagesBotResults> results);

        /// <summary>
        ///  Set the result of the <see cref="Action{bool}"/> as the result of sending a <see cref="UpdateBotInlineSend"/> representing
        ///  a selection from an Inline Mode query.
        /// </summary>
        /// <param name="selection">The <see cref="UpdateBotInlineSend"/> representing the selection from an Inline Mode query.</param>
        /// <param name="success">True if sending the selection succeeded, False if not.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{bool}"/></returns>
        Task SendBotInlineModeSelection(UpdateBotInlineSend selection, Action<bool> success);

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
