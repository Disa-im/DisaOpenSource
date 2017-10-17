using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : BubbleGroupsSync.Agent
    {
        public Task<List<BubbleGroup>> LoadBubbleGroups(BubbleGroup startGroup, int count = 10, 
                                                        BubbleGroupsSync.Category category = null)
        {
            return Task<List<BubbleGroup>>.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    var iDialogs =
                        TelegramUtils.RunSynchronously(client.Client.
                                                       Methods.MessagesGetDialogsAsync(new MessagesGetDialogsArgs
                        {
                            Limit = 100,
                            OffsetPeer = new InputPeerEmpty(),
                        }));
                    var bubbleGroups = new List<BubbleGroup>();
                    var messagesDialogs = iDialogs as MessagesDialogs;
                    if (messagesDialogs != null)
                    {
                        var next = false;
                        foreach (var idialog in messagesDialogs.Dialogs)
                        {
                            var dialog = idialog as Dialog;
                            if (dialog != null)
                            {
                                var bubbles = new List<VisualBubble>();
                                var iMessage = FindMessage(dialog.TopMessage, messagesDialogs.Messages);
                                var message = iMessage as Message;
                                var messageService = iMessage as MessageService;
                                if (message != null)
                                {
                                    var subBubbles = ProcessFullMessage(message, false, client.Client);
                                    bubbles.AddRange(subBubbles);
                                }
                                if (messageService != null)
                                {
                                    var subBubbles = 
                                        MakePartyInformationBubble(messageService, false, client.Client);
                                    bubbles.AddRange(subBubbles);                                   
                                }
                                var firstBubble = bubbles.FirstOrDefault();
                                if (firstBubble != null)
                                {
                                    if (next)
                                    {
                                        var bubbleGroup = new BubbleGroup(bubbles, true);
                                        bubbleGroups.Add(bubbleGroup);
                                        if (bubbleGroups.Count >= count)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (firstBubble.Address == startGroup.Address)
                                        {
                                            next = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        //TODO:
                    }
                    return bubbleGroups;
                }
            });
        }
    }
}
