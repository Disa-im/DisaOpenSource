using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;
using SharpTelegram;
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
					lock(_globalBubbleLock)
					{
						var bubbles = LoadBubblesForBubbleGroup(group, Time.GetNowUnixTimestamp(), limit);
						if (bubbles.LastOrDefault() != null)
						{
							var lastActionId = bubbles.LastOrDefault().IdService;
							return new BubbleGroupSync.Result(BubbleGroupSync.Result.Type.Purge, lastActionId, null, bubbles.ToArray());
						}
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
			lock(_globalBubbleLock)
			{
				var response = GetMessageHistory(group, fromTime, max);
				var messages = response as MessagesMessages;
				var messagesSlice = response as MessagesMessagesSlice;
				var messagesChannels = response as MessagesChannelMessages;
				if (messages != null)
				{
					_dialogs.AddChats(messages.Chats);
					_dialogs.AddUsers(messages.Users);
					messages.Messages.Reverse();
					return ConvertMessageToBubbles(messages.Messages);

				}
				if (messagesSlice != null)
				{
					_dialogs.AddChats(messagesSlice.Chats);
					_dialogs.AddUsers(messagesSlice.Users);
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

                    Action<List<IMessage>, string> changeAddress = (messages, address) =>
                    {
                        foreach (var message in messages)
                        {
                            var normalMessage = message as Message;
                            var serviceMessage = message as MessageService;

                            if (normalMessage != null)
                            {
                                normalMessage.ToId = new PeerChannel
                                {
                                    ChannelId = uint.Parse(address)
                                };
                            }
                            if (serviceMessage != null)
                            {
                                serviceMessage.ToId = new PeerChannel
                                {
                                    ChannelId = uint.Parse(address)
                                };
                            }
                        }
                    };

                    Action<IMessagesMessages,string> updateChatMessageAddresses = (chatMessages, address) =>
                    {
                        var messagesChats = chatMessages as MessagesMessages;
                        var messagesChatsSlice = chatMessages as MessagesMessagesSlice;

                        if (messagesChats != null)
                        {
                            changeAddress(messagesChats.Messages, address);
                        }
                        if (messagesChatsSlice != null)
                        {
                            changeAddress(messagesChatsSlice.Messages, address);
                        }
                    };


                    do
                    {
                        if (isNewBubbleGroup(group))
                        {
                            var newOffsetId = GetLastPtsForChannel(client.Client, group.Address);
                            SaveChannelState(uint.Parse(group.Address), newOffsetId); //save the state for this channel since it wount have any as its a new bubblegroup
                            finalMessages = GetChannelMessages(group, newOffsetId, max, client);
                            if (hasSuperGroupCreatedMessage(finalMessages))
                            {
                                var remainingCount = getMessagesCount(finalMessages); 
                                if (max - remainingCount > 0)
                                {
                                    var chatMessages = GetChatMessagesForChannel(group, Time.GetNowUnixTimestamp(), max - remainingCount , client);
                                    updateChatMessageAddresses(chatMessages, group.Address);
                                    MergeMessagesMessages(finalMessages, chatMessages);
                                }
                            }
                            break;
                        }

                        bool queryChat;
                        bool lastMessageIsExtendedParty;

                        var offsetId = GetMessagePtsForTime(group, fromTime, out queryChat, out lastMessageIsExtendedParty);

                        if (queryChat)
                        {
                            if (lastMessageIsExtendedParty)
                            {
                                finalMessages = GetChannelMessages(group, 1, max, client);
                                SaveChannelState(uint.Parse(group.Address), 1); //this group was just upgraded to an extended party, we need to explicity save its state otherwise itll be zero
                            }
                            var chatMessages =  GetChatMessagesForChannel(group, fromTime, max, client);
                            updateChatMessageAddresses(chatMessages, group.Address);
                            MergeMessagesMessages(finalMessages, chatMessages);
                            break;
                        }

                        finalMessages = GetChannelMessages(group, offsetId, max, client);
                        var finalMessagesCount = getMessagesCount(finalMessages);
                        if (max - finalMessagesCount > 0)
                        {
                            var chatMessages = GetChatMessagesForChannel(group, fromTime, max - finalMessagesCount, client);
                            updateChatMessageAddresses(chatMessages, group.Address);
                            MergeMessagesMessages(finalMessages, chatMessages);
                        }

                    } while (false);

                    return finalMessages;
                }
                else
                {
                    var peer = GetInputPeer(group.Address, group.IsParty, group.IsExtendedParty);

                    bool queryChat;
                    bool lastMessageIsExtendedParty;

                    var offsetId = GetMessagePtsForTime(group, fromTime, out queryChat, out lastMessageIsExtendedParty);

                    if (offsetId != 1)
                    {

                        var response =
                            TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesGetHistoryAsync(new MessagesGetHistoryArgs
                                {
                                    Peer = peer,
                                    OffsetId = offsetId + 1,
                                    Limit = (uint)max
                                }));
                        return response;
                    }
                    else
                    {
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

        }

        private uint GetLastPtsForChannel(TelegramClient client, string address)
        {
            uint offset = 0;

            uint pts = 0;

        Again:

            var iDialogs = TelegramUtils.RunSynchronously(client.Methods.MessagesGetDialogsAsync(new MessagesGetDialogsArgs
            {
                Limit = 100,
                OffsetPeer = new InputPeerEmpty()
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
                //pts can never be zero, so we use this
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
                var dialogObj = dialog as Dialog;
                if (dialogObj != null)
                {
                    if (TelegramUtils.GetPeerId(dialogObj.Peer) == address)
                    {
                        return dialogObj.Pts;
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
                    DisposeFullChat();
                    return TelegramUtils.RunSynchronously(
                            client.Client.Methods.MessagesGetHistoryAsync(new MessagesGetHistoryArgs
                            {
                                Peer = peerChat,
                                OffsetDate = (uint)fromTime,
                                Limit = (uint)max

                            }));
                }
                DisposeFullChat();
                return MakeMessagesMessages(new List<IChat>(), new List<IUser>(), new List<IMessage>());
            }
            DisposeFullChat();
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

        private uint GetMessagePtsForTime(BubbleGroup group, long fromTime, out bool queryChat, out bool lastMessageIsExtendedParty)
        {
            VisualBubble bubbleToSyncFrom = null;
            bool localQueryChat = false;
            bool localLastMessageIsExtendedParty = false;

            foreach (var bubble in BubbleGroupSync.ReadBubblesFromDatabase(group))
            {
                if (bubble is PartyInformationBubble)
                {
                    if ((bubble as PartyInformationBubble).Type == PartyInformationBubble.InformationType.UpgradedToExtendedParty)
                    {
                        localQueryChat = true;
                        if (bubble.Time <= fromTime)
                        {
                            localLastMessageIsExtendedParty = true;
                            bubbleToSyncFrom = bubble;
                            break;
                        }
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
                    lastMessageIsExtendedParty = localLastMessageIsExtendedParty;
                    return 1;
                }
                queryChat = localQueryChat;
                lastMessageIsExtendedParty = localLastMessageIsExtendedParty;
                return uint.Parse(bubbleToSyncFrom.IdService);
            }
            queryChat = localQueryChat;
            lastMessageIsExtendedParty = localLastMessageIsExtendedParty;
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
                    var messageBubbles = ProcessFullMessage(message, false);
                    var i = 0;
                    foreach (var bubble in messageBubbles)
                    {
                        if (message.ReplyToMsgId != 0 && i == 0)//add quoted message only to the first bubble
                        {
                            var iReplyMessage = GetMessage(message.ReplyToMsgId, null, uint.Parse(TelegramUtils.GetPeerId(message.ToId)), message.ToId is PeerChannel);
                            var replyMessage = iReplyMessage as Message;
                            AddQuotedMessageToBubble(replyMessage, bubble);
                        }
                        bubbles.Add(bubble);
                        i++;
                    }
                }
                if (messageService != null)
                {
                    _oldMessages = true;
                    var serviceBubbles = MakePartyInformationBubble(messageService, false, null);
                    if (serviceBubbles != null)
                    {
                        bubbles.AddRange(serviceBubbles);
                    }
                    _oldMessages = false;
                }
            }
            return bubbles;
        }
    }
}
