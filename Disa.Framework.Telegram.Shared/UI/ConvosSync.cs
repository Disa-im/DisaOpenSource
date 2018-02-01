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
        public Task<bool> OnLazyBubbleGroupsDeleted(List<BubbleGroup> groups)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                return true;
            });
        }

        Task<IEnumerable<BubbleGroup>> BubbleGroupsSync.Agent.LoadBubbleGroups(IEnumerable<Tag> tags)
        {
            throw new NotImplementedException();
        }

        //Task<List<VisualBubble>> BubbleGroupsSync.Agent.LoadBubbleGroups2(BubbleGroup startGroup, int count, IEnumerable<Tag> tags)
        //{
        //    return Task<List<VisualBubble>>.Factory.StartNew(() =>
        //    {
        //        using (var client = new FullClientDisposable(this))
        //        {
        //            var iDialogs =
        //                TelegramUtils.RunSynchronously(client.Client.
        //                                               Methods.MessagesGetDialogsAsync(new MessagesGetDialogsArgs
        //                                               {
        //                                                   Limit = 100,
        //                                                   OffsetPeer = new InputPeerEmpty(),
        //                                               }));
        //            var returnBubbles = new List<VisualBubble>();
        //            var messagesDialogs = iDialogs as MessagesDialogs;
        //            if (messagesDialogs != null)
        //            {
        //                var next = false;
        //                var counts = 0;
        //                foreach (var idialog in messagesDialogs.Dialogs)
        //                {
        //                    var dialog = idialog as Dialog;
        //                    if (dialog != null)
        //                    {
        //                        var bubbles = new List<VisualBubble>();
        //                        var iMessage = FindMessage(dialog.TopMessage, dialog.Peer, messagesDialogs.Messages);
        //                        var message = iMessage as Message;
        //                        var messageService = iMessage as MessageService;
        //                        if (message != null)
        //                        {
        //                            var subBubbles = ProcessFullMessage(message, false, client.Client);
        //                            bubbles.AddRange(subBubbles);
        //                        }
        //                        if (messageService != null)
        //                        {
        //                            var subBubbles =
        //                                MakePartyInformationBubble(messageService, false, client.Client);
        //                            bubbles.AddRange(subBubbles);
        //                        }
        //                        var firstBubble = bubbles.FirstOrDefault();
        //                        if (firstBubble != null)
        //                        {
        //                            if (next)
        //                            {
        //                                returnBubbles.Add(firstBubble);
        //                                if (returnBubbles.Count >= count)
        //                                {
        //                                    break;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                if (firstBubble.Address == startGroup.Address)
        //                                {
        //                                    next = true;
        //                                }
        //                            }
        //                        }
        //                    }
        //                    counts++;
        //                }
        //            }
        //            else
        //            {
        //                //TODO:
        //            }
        //            return returnBubbles;
        //        }
        //    });
        //}
    }
}
