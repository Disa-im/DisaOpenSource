namespace Disa.Framework.Bots
{
    /// <summary>
    /// Represents the ability for a user to switch to a private mode chat with the bot in Inline Mode.
    /// 
    /// Example: An inline mode bot that sends YouTube videos can ask the user to connect the bot to 
    /// their YouTube account to adapt search results accordingly. To do this, it displays a 
    /// ‘Connect your YouTube account’ button above the results, or even before showing any. 
    /// The user presses the button, switches to a private chat with the bot and, in doing so, 
    /// passes a start parameter that instructs the bot to return an oauth link. Once done, the
    /// bot can offer a Switch Inline button so that the user can easily return to the chat where 
    /// they wanted to use the bot's inline capabilities.
    /// </summary>
    public class InlineBotSwitchPM
    {
        /// <summary>
        /// If passed, the Disa client will display a button with specified text that switches the user 
        /// to a private chat with the bot and sends the bot a start message with the parameter <see cref="StartParam"/>.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Deep-linking parameter for the /start message sent to the bot when user presses the switch button.
        /// 1-64 characters, only A-Z, a-z, 0-9, _ and - are allowed.
        /// </summary>
        public string StartParam { get; set; }
    }
}
