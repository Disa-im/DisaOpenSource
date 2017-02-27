using Disa.Framework.Bot;
using SharpMTProto;
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
                // Only handling usernames now
                if (token != "@")
                {
                    result(new List<Mention>());
                }

                var fullChat = MentionsFetchFullChat(group.Address, group.IsExtendedParty);
                var partyParticipants = MentionsGetPartyParticipants(fullChat);

                var resultList = new List<Mention>();
                if (!group.IsExtendedParty)
                {
                    foreach (var partyParticipant in partyParticipants.ChatParticipants)
                    {
                        var id = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                        if (id != null)
                        {
                            var user = _dialogs.GetUser(uint.Parse(id));
                            var username = TelegramUtils.GetUserHandle(user);
                            var name = TelegramUtils.GetUserName(user);
                            var mention  = new UsernameMention
                            {
                                Token = "@",
                                BubbleGroupId = group.ID,
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
                                BubbleGroupId = group.ID,
                                Value = username,
                                Name = name,
                                Address = id
                            };

                            resultList.Add(mention);
                        }
                    }
                }

                result(resultList);
            });
        }

        // TODO
        public Task GetRecentHashtags(Action<List<Hashtag>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(new List<Hashtag>());
            });
        }

        // TODO
        public Task SetRecentHashtags(List<Hashtag> hashtags, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        // TODO
        public Task ClearRecentHashtags(Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        // TODO
        public Task GetContactsByUsername(string username, Action<List<Contact>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var peer = ResolvePeer(username);

                var contacts = new List<Contact>();
                foreach (var peerUser in peer.Users)
                {
                    var user = peerUser as User;
                    if (user != null)
                    {
                        var contact = CreateTelegramContact(user);
                        contacts.Add(contact);
                    }
                }

                result(contacts);
            });
        }

        // TODO
        public Task GetInlineBotResults(BotContact bot, string query, string offset, Action<BotResults> botResults)
        {
            throw new NotImplementedException();
        }

        // Separate implementation for Mentions - PartyOptions has its own as well
        private MessagesChatFull MentionsFetchFullChat(string address, bool superGroup)
        {
            MessagesChatFull fullChat = null;
            using (var client = new FullClientDisposable(this))
            {
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
                    try
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
                    catch (Exception e)
                    {
                        DebugPrint(">>>> get full channel exception " + e);
                    }
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

