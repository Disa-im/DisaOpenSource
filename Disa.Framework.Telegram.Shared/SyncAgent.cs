using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : BubbleGroupSync.Agent
    {
        public Task<BubbleGroupSync.Result> Sync(BubbleGroup @group, string actionId)
        {
            return Task<BubbleGroupSync.Result>.Factory.StartNew(() =>
            {
                const int limit = 100;
                var actionIdLong = actionId == null ? 0 : long.Parse(actionId);

                if (actionIdLong == 0)
                {
                   var bubbles = LoadBubblesForBubbleGroup(group, Time.GetNowUnixTimestamp(), limit);
                    if (bubbles.LastOrDefault() != null)
                    {
                        var lastActionId = bubbles.LastOrDefault().IdService;
                        return new BubbleGroupSync.Result(BubbleGroupSync.Result.Type.Purge,lastActionId,null,bubbles.ToArray());
                    }
                }
                return new BubbleGroupSync.Result();
            });
        }

        public Task<bool> DeleteBubble(BubbleGroup @group, VisualBubble bubble)
        {
            //just basic true, since we plan on not implementing this stuff
            return Task<bool>.Factory.StartNew(() => true);
        }

        public Task<bool> DeleteConversation(BubbleGroup @group)
        {
            return Task<bool>.Factory.StartNew(() => true);
        }

        public Task<List<VisualBubble>> LoadBubbles(BubbleGroup @group, long fromTime, int max = 100)
        {
            return Task<List<VisualBubble>>.Factory.StartNew(() =>
            {
                return LoadBubblesForBubbleGroup(group, fromTime, max);
            });
        }

        private List<VisualBubble> LoadBubblesForBubbleGroup(BubbleGroup @group, long fromTime, int max)
        {
            var response = GetMessageHistory(group, fromTime, max);
            var messages = response as MessagesMessages;
            var messagesSlice = response as MessagesMessagesSlice;
            var messagesChannels = response as MessagesChannelMessages;
            if (messages != null)
            {
                _dialogs.AddChats(messages.Chats);
                _dialogs.AddUsers(messages.Users);
                //DebugPrint("Messages are as follows " + ObjectDumper.Dump(messages.Messages));
                messages.Messages.Reverse();
                return ConvertMessageToBubbles(messages.Messages);

            }
            if (messagesSlice != null)
            {
                _dialogs.AddChats(messagesSlice.Chats);
                _dialogs.AddUsers(messagesSlice.Users);
                //DebugPrint("Messages are as follows " + ObjectDumper.Dump(messagesSlice.Messages));
                messagesSlice.Messages.Reverse();
                return ConvertMessageToBubbles(messagesSlice.Messages);
            }
            if (messagesChannels != null)
            {
                _dialogs.AddChats(messagesChannels.Chats);
                _dialogs.AddUsers(messagesChannels.Users);
                messagesChannels.Messages.Reverse();
                return ConvertMessageToBubbles(messagesChannels.Messages);
            }
            return new List<VisualBubble>();
        }

        private IMessagesMessages GetMessageHistory(BubbleGroup group, long fromTime, int max)
        {
            using (var client = new FullClientDisposable(this))
            {
                if (group.IsExtendedParty)
                {
                    try
                    {
                        var offsetId = GetMessagePtsForTime(group, fromTime);
                        var peer = GetInputPeer(group.Address, group.IsParty, group.IsExtendedParty);
                        var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                        var response =
                            TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesGetHistoryAsync(new MessagesGetHistoryArgs
                                {
                                    Peer = peer,
                                    OffsetId = offsetId+1,
                                    OffsetDate = 0,
                                    Limit = 50

                                }));

                        return response;
                    }
                    catch (Exception e)
                    {
                        DebugPrint("Exception " + e);
                        return null;
                    }
                }
                else
                {
                    var peer = GetInputPeer(group.Address, group.IsParty, group.IsExtendedParty);
                    var response =
                        TelegramUtils.RunSynchronously(
                            client.Client.Methods.MessagesGetHistoryAsync(new MessagesGetHistoryArgs
                            {
                                Peer = peer,
                                OffsetDate = (uint)fromTime,
                                Limit = (uint)max

                            }));
                    return response;
                }
            }
        }

        private MessagesMessages MakeMessagesMessages(List<IChat> chats, List<IUser> users, List<IMessage> messages)
        {
            return new MessagesMessages
            {
                Chats = chats,
                Users = users,
                Messages = messages
            };
        }

        private uint GetMessagePtsForTime(BubbleGroup group, long fromTime)
        {
            VisualBubble bubbleToSyncFrom = null;
            foreach (var bubble in BubbleGroupSync.ReadBubblesFromDatabase(group))
            {
                if (bubble.Time <= fromTime)
                {
                    bubbleToSyncFrom = bubble;
                    break;
                }
            }
            if (bubbleToSyncFrom != null)
            {
                if (bubbleToSyncFrom.IdService == null)
                {
                    return 1;
                }
                return uint.Parse(bubbleToSyncFrom.IdService);
            }
            return 1;
        }

        private List<VisualBubble> ConvertMessageToBubbles(List<IMessage> messages)
        {
            var bubbles = new List<VisualBubble>();
            foreach (var iMessage in messages)
            {
                var message = iMessage as Message;
                if(message==null)continue;
                var bubble = ProcessFullMessage(message, false);
				if (bubble != null)
				{
					if (message.ReplyToMsgId != 0)
					{
                        var iReplyMessage = GetMessage(message.ReplyToMsgId, null);
						DebugPrint(">>> got message " + ObjectDumper.Dump(iReplyMessage));
						var replyMessage = iReplyMessage as Message;
						AddQuotedMessageToBubble(replyMessage, bubble);
					}
					bubbles.Add(bubble);
				}
            }
            return bubbles;
        }
    }
}
