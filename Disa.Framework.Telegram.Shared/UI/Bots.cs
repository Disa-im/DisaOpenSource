using Disa.Framework.Bots;
using Disa.Framework.Bubbles;
using SharpTelegram.Schema;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IBots
    {
        public Task GetBotCallbackAnswer(BubbleGroup group, VisualBubble bubble, Bots.KeyboardButton button, Action<Bots.MessagesBotCallbackAnswer> answer)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    // Note: Telegram also has KeyboardButtonGame which functions as a callback also
                    if (button is Bots.KeyboardButtonCallback)
                    {
                        var keyboardButtonCallback = button as Bots.KeyboardButtonCallback;

                        var args = new MessagesGetBotCallbackAnswerArgs
                        {
                            MsgId = uint.Parse(bubble.IdService),
                            Data = keyboardButtonCallback.Data,
                            Peer = GetInputPeer(group.Address, group.IsParty, group.IsExtendedParty)
                        };

                        SharpTelegram.Schema.MessagesBotCallbackAnswer telegramBotCallbackAnswer =
                            (SharpTelegram.Schema.MessagesBotCallbackAnswer)
                                TelegramUtils.RunSynchronously(
                                    client.Client.Methods.MessagesGetBotCallbackAnswerAsync(args));

                        var disaBotCallbackAnswer = new Bots.MessagesBotCallbackAnswer
                        {
                            Alert = telegramBotCallbackAnswer.Alert != null ? true : false,
                            Message = telegramBotCallbackAnswer.Message
                        };

                        answer(disaBotCallbackAnswer);
                    }
                }
            });
        }
    }
}
