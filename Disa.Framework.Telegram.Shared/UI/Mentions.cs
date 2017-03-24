using SharpMTProto;
using SharpTelegram;
using SharpTelegram.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IMentions
    {
        public Task GetToken(MentionType mentionType, Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                switch (mentionType)
                {
                    case MentionType.Username:
                        {
                            result("@");
                            break;
                        }
                    case MentionType.Hashtag:
                        {
                            result("#");
                            break;
                        }
                    case MentionType.BotCommand:
                        {
                            result(@"/");
                            break;
                        }
                }
            });
        }

        // Usernames - we need to return usernames and names for a particular group
        // Hasthags - we need to return most recent hashtags across ALL groups
        // BotCommand - we need to return a bot usernames and commands for a particular group
        public Task GetMentions(string token, BubbleGroup group, Action<List<Mention>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(MentionsGetMentions(token, group.Address, group.IsExtendedParty));
            });
        }

        private List<Mention> MentionsGetMentions(string token, string address, bool isChannel, TelegramClient optionalClient = null)
        {
            // Only handling usernames now
            if (token != "@")
            {
                return new List<Mention>();
            }

            var fullChat = MentionsFetchFullChat(address, isChannel, optionalClient);
            var partyParticipants = MentionsGetPartyParticipants(fullChat);

            var resultList = new List<Mention>();
            if (!isChannel)
            {
                foreach (var partyParticipant in partyParticipants.ChatParticipants)
                {
                    var id = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                    if (id != null)
                    {
                        var user = _dialogs.GetUser(uint.Parse(id));
                        var username = TelegramUtils.GetUserHandle(user);
                        var name = TelegramUtils.GetUserName(user);
                        var mention = new UsernameMention
                        {
                            Token = "@",
                            Value = username,
                            Name = name,
                            Address = id
                        };
                        resultList.Add(mention);
                    }
                }
            }
            else
            {
                foreach (var partyParticipant in partyParticipants.ChannelParticipants)
                {
                    var id = TelegramUtils.GetUserIdFromChannelParticipant(partyParticipant);
                    if (id != null)
                    {
                        var user = _dialogs.GetUser(uint.Parse(id));
                        var username = TelegramUtils.GetUserHandle(user);
                        var name = TelegramUtils.GetUserName(user);
                        var mention = new UsernameMention
                        {
                            Token = "@",
                            Value = username,
                            Name = name,
                            Address = id
                        };

                        resultList.Add(mention);
                    }
                }
            }

            return resultList;
        }

        // Separate implementation for Mentions - PartyOptions has its own as well
        private MessagesChatFull MentionsFetchFullChat(string address, bool superGroup, TelegramClient optionalClient = null)
        {
            using (var client = new OptionalClientDisposable(this, optionalClient))
            {
                MessagesChatFull fullChat = null;

                if (!superGroup)
                {
                    fullChat =
                        (MessagesChatFull)
                            TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesGetFullChatAsync(new MessagesGetFullChatArgs
                                {
                                    ChatId = uint.Parse(address)
                                }));
                }
                else
                {
                    fullChat =
                        (MessagesChatFull)
                            TelegramUtils.RunSynchronously(
                            client.Client.Methods.ChannelsGetFullChannelAsync(new ChannelsGetFullChannelArgs
                            {
                                Channel = new InputChannel
                                {
                                    ChannelId = uint.Parse(address),
                                    AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(address)))
                                }
                            }));
                }

                _dialogs.AddUsers(fullChat.Users);
                _dialogs.AddChats(fullChat.Chats);

                return fullChat;
            }
        }

        // Separate implementation for Mentions - PartyOptions has its own as well
        private Participants MentionsGetPartyParticipants(MessagesChatFull fullChat)
        {
            Participants participants = null;

            var iChatFull = fullChat.FullChat;
            var chatFull = iChatFull as ChatFull;
            var channelFull = iChatFull as ChannelFull;
            if (chatFull != null)
            {
                var chatParticipants = chatFull.Participants as ChatParticipants;
                if (chatParticipants != null)
                {
                    participants = new Participants
                    {
                        Type = ParticipantsType.Chat,
                        ChatParticipants = chatParticipants.Participants
                    };
                    return participants;
                }
            }
            if (channelFull != null)
            {
                if (channelFull.CanViewParticipants == null)
                {
                    return new Participants
                    {
                        Type = ParticipantsType.Channel,
                        ChannelParticipants = new List<IChannelParticipant>()
                    };
                }

                var channelParticipants = GetChannelParticipants(channelFull, new ChannelParticipantsRecent());
                var channelAdmins = GetChannelParticipants(channelFull, new ChannelParticipantsAdmins());
                var mergedList = channelAdmins.Union(channelParticipants, new ChannelParticipantComparer()).ToList();
                participants = new Participants
                {
                    Type = ParticipantsType.Channel,
                    ChannelParticipants = mergedList
                };
                return participants;
            }

            return null;    
        }
    }
}

