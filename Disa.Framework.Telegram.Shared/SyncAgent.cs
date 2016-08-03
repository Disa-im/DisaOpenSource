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
                var bubbles = ConvertMessageToBubbles(messagesChannels.Messages);
                SetExtendedFlag(bubbles);
                return bubbles;
            }
            return new List<VisualBubble>();
        }

        private void SetExtendedFlag(List<VisualBubble> bubbles)
        {
            foreach (var bubble in bubbles)
            {
                bubble.ExtendedParty = true;
            }
        }

        private IMessagesMessages GetMessageHistory(BubbleGroup group, long fromTime, int max)
        {
            using (var client = new FullClientDisposable(this))
            {
                if (group.IsExtendedParty)
                {
                    IMessagesMessages finalMessages = MakeMessagesMessages(new List<IChat>(), new List<IUser>(), new List<IMessage>());

                    Func <BubbleGroup, bool> isNewBubbleGroup = bubbleGroup =>
                    {
                        var bubbles = BubbleGroupSync.ReadBubblesFromDatabase(group);
                        if (bubbles.Count() > 1)
                        {
                            return false;
                        }
                        foreach (var bubble in bubbles)
                        {
                            if (bubble is NewBubble)
                            {
                                return true;
                            }
                        }
                        return false;
                    };

                    Func<IMessagesMessages, bool> hasSuperGroupCreatedMessage = messages =>
                    {
                        var messagesChannelMessages = messages as MessagesChannelMessages;
                        if (messagesChannelMessages != null)
                        {
                            foreach (var message in messagesChannelMessages.Messages)
                            {
                                var messageService = message as MessageService;
                                if (messageService == null) continue;
                                if (messageService.Action is MessageActionChannelMigrateFrom)
                                {
                                    return true;
                                }
                            }
                        }
                        return false;
                    };

                    Func<IMessagesMessages, int> getMessagesCount = messages =>
                    {
                        var messagesChats = messages as MessagesMessages;
                        var messagesChatsSlice = messages as MessagesMessagesSlice;
                        var messagesChatsChannel = messages as MessagesChannelMessages;
                        if (messagesChats != null)
                        {
                            return messagesChats.Messages.Count;
                        }
                        if (messagesChatsSlice != null)
                        {
                            return messagesChats.Messages.Count;
                        }
                        if (messagesChatsChannel != null)
                        {
                            return messagesChatsChannel.Messages.Count;
                        }
                        return 0;
                    };

                    do
                    {
                        if (isNewBubbleGroup(group))
                        {
                            var newOffsetId = GetLastPtsForChannel(client, group.Address);
                            finalMessages = GetChannelMessages(group, newOffsetId, max, client);
                            if (hasSuperGroupCreatedMessage(finalMessages))
                            {
                                var remainingCount = getMessagesCount(finalMessages); 
                                if (max - remainingCount > 0)
                                {
                                    var chatMessages = GetChatMessagesForChannel(group, Time.GetNowUnixTimestamp(), max - remainingCount , client);
                                    MergeMessagesMessages(finalMessages, chatMessages);
                                }
                            }
                            break;
                        }

                        bool queryChat;

                        var offsetId = GetMessagePtsForTime(group, fromTime, out queryChat);

                        if (queryChat)
                        {
                            var chatMessages =  GetChatMessagesForChannel(group, fromTime, max, client);
                            MergeMessagesMessages(finalMessages, chatMessages);
                            break;
                        }

                        finalMessages = GetChannelMessages(group, offsetId, max, client);
                        var finalMessagesCount = getMessagesCount(finalMessages);
                        if (max - finalMessagesCount > 0)
                        {
                            var chatMessages = GetChatMessagesForChannel(group, fromTime, max - finalMessagesCount, client);
                            MergeMessagesMessages(finalMessages, chatMessages);
                        }

                    } while (false);

                    return finalMessages;
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

        private uint GetLastPtsForChannel(FullClientDisposable client, string address)
        {
            uint offset = 0;

            uint pts = 0;

        Again:

            var iDialogs = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsGetDialogsAsync(new ChannelsGetDialogsArgs
            {
                Limit = 100,
                Offset = offset
            }));
            var dialogs = iDialogs as MessagesDialogs;
            var dialogsSlice = iDialogs as MessagesDialogsSlice;
            if (dialogs != null)
            {
                pts = FindPtsFromDialogs(dialogs.Dialogs, address);
            }
            if (dialogsSlice != null)
            {
                pts  = FindPtsFromDialogs(dialogsSlice.Dialogs, address);
                if (pts == 0)
                {
                    offset += dialogsSlice.Count;
                    goto Again;
                }
            }

            return pts;
        }

        private uint FindPtsFromDialogs(List<IDialog> dialogs, string address)
        {
            foreach (var dialog in dialogs)
            {
                var dialogChannel = dialog as DialogChannel;
                if (dialogChannel != null)
                {
                    if (TelegramUtils.GetPeerId(dialogChannel.Peer) == address)
                    {
                        return dialogChannel.Pts;
                    }
                }
            }
            return 0;
        }

        private IMessagesMessages GetChannelMessages(BubbleGroup group, uint offsetId, int max, FullClientDisposable client)
        {
            var peer = GetInputPeer(group.Address, group.IsParty, group.IsExtendedParty);
            return 
                TelegramUtils.RunSynchronously(
                        client.Client.Methods.MessagesGetHistoryAsync(new MessagesGetHistoryArgs
                        {
                            Peer = peer,
                            OffsetId = offsetId + 1,
                            OffsetDate = 0,
                            Limit = (uint)max
                        }));
        }

        private IMessagesMessages GetChatMessagesForChannel(BubbleGroup group, long fromTime, int max, FullClientDisposable client)
        {
            var fullChat = FetchFullChat(group.Address, true);
            var fullChannel = fullChat.FullChat as ChannelFull;
            if (fullChannel != null)
            {
                if (fullChannel.MigratedFromChatId != 0)
                {
                    var peerChat = GetInputPeer(fullChannel.MigratedFromChatId.ToString(), true, false);
                    return TelegramUtils.RunSynchronously(
                            client.Client.Methods.MessagesGetHistoryAsync(new MessagesGetHistoryArgs
                            {
                                Peer = peerChat,
                                OffsetDate = (uint)fromTime,
                                Limit = (uint)max

                            }));
                }
                return MakeMessagesMessages(new List<IChat>(), new List<IUser>(), new List<IMessage>());
            }
            return MakeMessagesMessages(new List<IChat>(), new List<IUser>(), new List<IMessage>());
        }

        private bool IsNewBubbleGroup(BubbleGroup group)
        {
            throw new NotImplementedException();
        }

        private IMessagesMessages MergeMessagesMessages(IMessagesMessages iMessagesChannel, IMessagesMessages iMessagesChat)
        {
            var messagesChannel = iMessagesChannel as MessagesChannelMessages;
            if (messagesChannel != null)
            {
                var messagesChat = iMessagesChat as MessagesMessages;
                var messagesChatSlice = iMessagesChat as MessagesMessagesSlice;

                if (messagesChat != null)
                {
                    messagesChannel.Chats.AddRange(messagesChat.Chats);
                    messagesChannel.Users.AddRange(messagesChat.Users);
                    messagesChannel.Messages.AddRange(messagesChat.Messages);
                }
                if(messagesChatSlice != null)
                {
                    messagesChannel.Chats.AddRange(messagesChatSlice.Chats);
                    messagesChannel.Users.AddRange(messagesChatSlice.Users);
                    messagesChannel.Messages.AddRange(messagesChatSlice.Messages);
                }
            }
            return messagesChannel;
        }

        private MessagesChannelMessages MakeMessagesMessages(List<IChat> chats, List<IUser> users, List<IMessage> messages)
        {
            return new MessagesChannelMessages
            {
                Chats = chats,
                Messages = messages,
                Users = users,
            };
        }

        private uint GetMessagePtsForTime(BubbleGroup group, long fromTime, out bool queryChat)
        {
            VisualBubble bubbleToSyncFrom = null;
            bool localQueryChat = false;
            foreach (var bubble in BubbleGroupSync.ReadBubblesFromDatabase(group))
            {
                if (bubble is PartyInformationBubble)
                {
                    if ((bubble as PartyInformationBubble).Type == PartyInformationBubble.InformationType.UpgradedToExtendedParty)
                    {
                        localQueryChat = true;
                    }
                }
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
                    queryChat = localQueryChat;
                    return 1;
                }
                queryChat = localQueryChat;
                return uint.Parse(bubbleToSyncFrom.IdService);
            }
            queryChat = localQueryChat;
            return 1;
        }

        private List<VisualBubble> ConvertMessageToBubbles(List<IMessage> messages)
        {
            var bubbles = new List<VisualBubble>();
            foreach (var iMessage in messages)
            {
                var message = iMessage as Message;
                var messageService = iMessage as MessageService;
                if (message != null)
                {
                    var bubble = ProcessFullMessage(message, false);
                    if (bubble != null)
                    {
                        if (message.ReplyToMsgId != 0)
                        {
                            var iReplyMessage = GetMessage(message.ReplyToMsgId, null, uint.Parse(TelegramUtils.GetPeerId(message.ToId)), message.ToId is PeerChannel);
                            DebugPrint(">>> got message " + ObjectDumper.Dump(iReplyMessage));
                            var replyMessage = iReplyMessage as Message;
                            AddQuotedMessageToBubble(replyMessage, bubble);
                        }
                        bubbles.Add(bubble);
                    }
                }
                if (messageService != null)
                {
                    var serviceBubbles = MakePartyInformationBubble(messageService, false);
                    if (serviceBubbles != null)
                    {
                        bubbles.AddRange(serviceBubbles);
                    }
                }
            }
            return bubbles;
        }
    }
}
