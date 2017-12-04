using SharpTelegram;
using SharpTelegram.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IMentions
    {
        public Task GetTokens(Action<Dictionary<MentionType, char>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var tokens = new Dictionary<MentionType, char>();
                tokens.Add(MentionType.Username, '@');
                tokens.Add(MentionType.ContextBot, '@');
                tokens.Add(MentionType.BotCommand, '/');
                tokens.Add(MentionType.Hashtag, '#');

                result(tokens);
            });
        }

        // Usernames - we need to return usernames and names for a particular group
        // ContextBot - not supported yet
        // Hasthags - not supported yet
        // BotCommand - we need to return username, name and BotInfo for bots for a particular group
        public Task GetMentions(BubbleGroup group, Action<List<Mention>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                if (group.IsParty)
                {
                    GetPartyMentions(group, result);
                }
                else
                {
                    GetSoloMentions(group, result);
                }
            });
        }

        private void GetSoloMentions(BubbleGroup group, Action<List<Mention>> result)
        {
            using (var client = new FullClientDisposable(this))
            {
                var resultList = new List<Mention>();

                var user = _dialogs.GetUser(uint.Parse(group.Address));
                var inputUser = TelegramUtils.CastUserToInputUser(user);

                UserFull userFull =
                    (UserFull)
                        TelegramUtils.RunSynchronously(
                            client.Client.Methods.UsersGetFullUserAsync(new UsersGetFullUserArgs
                            {
                                Id = inputUser
                            }));

                if (userFull == null)
                {
                    result(resultList);
                }

                var telegramBotInfo = userFull.BotInfo as SharpTelegram.Schema.BotInfo;
                if (telegramBotInfo != null)
                {
                    var username = TelegramUtils.GetUserHandle(user);
                    var name = TelegramUtils.GetUserName(user);

                    var botCommandMention = new Mention
                    {
                        Type = MentionType.BotCommand,
                        BubbleGroupId = group.ID,
                        Value = username,
                        Name = name,
                        Address = telegramBotInfo.UserId.ToString(CultureInfo.InvariantCulture)
                    };

                    var disaBotInfo = new Disa.Framework.Bots.BotInfo
                    {
                        Address = telegramBotInfo.UserId.ToString(CultureInfo.InvariantCulture),
                        Description = telegramBotInfo.Description,
                        Commands = new List<Disa.Framework.Bots.BotCommand>()
                    };

                    foreach (var c in telegramBotInfo.Commands)
                    {
                        var telegramBotCommand = c as SharpTelegram.Schema.BotCommand;
                        if (telegramBotCommand != null)
                        {
                            disaBotInfo.Commands.Add(new Disa.Framework.Bots.BotCommand
                            {
                                Command = telegramBotCommand.Command,
                                Description = telegramBotCommand.Description
                            });
                        }
                    }

                    botCommandMention.BotInfo = disaBotInfo;

                    resultList.Add(botCommandMention);
                }

                result(resultList);
            }
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
                        var mention = new Mention
                        {
                            Type = MentionType.Username,
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
                foreach(var partyParticipant in partyParticipants.ChannelParticipants)
                {
                    var id = TelegramUtils.GetUserIdFromChannelParticipant(partyParticipant);
                    if (id != null)
                    {
                        var user = _dialogs.GetUser(uint.Parse(id));
                        var username = TelegramUtils.GetUserHandle(user);
                        var name = TelegramUtils.GetUserName(user);
                        var mention = new Mention
                        {
                            Type = MentionType.Username,
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

        private void GetPartyMentions(BubbleGroup group, Action<List<Mention>> result)
        {
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
                        var groupUsernameMention = new Mention
                        {
                            Type = MentionType.Username,
                            BubbleGroupId = group.ID,
                            Value = username,
                            Name = name,
                            Address = id
                        };
                        resultList.Add(groupUsernameMention);
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
                        var channelUsernameMention = new Mention
                        {
                            Type = MentionType.Username,
                            BubbleGroupId = group.ID,
                            Value = username,
                            Name = name,
                            Address = id
                        };

                        resultList.Add(channelUsernameMention);
                    }
                }
            }

            var chatFull = fullChat.FullChat as ChatFull;
            if (chatFull != null)
            {
                foreach (var chatFullBotInfo in chatFull.BotInfo)
                {
                    var telegramBotInfo = chatFullBotInfo as SharpTelegram.Schema.BotInfo;
                    if (telegramBotInfo != null)
                    {
                        var user = _dialogs.GetUser(telegramBotInfo.UserId);
                        var username = TelegramUtils.GetUserHandle(user);
                        var name = TelegramUtils.GetUserName(user);

                        var botCommandMention = new Mention
                        {
                            Type = MentionType.BotCommand,
                            BubbleGroupId = group.ID,
                            Value = username,
                            Name = name,
                            Address = telegramBotInfo.UserId.ToString(CultureInfo.InvariantCulture)
                        };

                        var disaBotInfo = new Disa.Framework.Bots.BotInfo
                        {
                            Address = telegramBotInfo.UserId.ToString(CultureInfo.InvariantCulture),
                            Description = telegramBotInfo.Description,
                            Commands = new List<Disa.Framework.Bots.BotCommand>()
                        };

                        foreach (var c in telegramBotInfo.Commands)
                        {
                            var telegramBotCommand = c as SharpTelegram.Schema.BotCommand;
                            if (telegramBotCommand != null)
                            {
                                disaBotInfo.Commands.Add(new Disa.Framework.Bots.BotCommand
                                {
                                    Command = telegramBotCommand.Command,
                                    Description = telegramBotCommand.Description
                                });
                            }
                        }

                        botCommandMention.BotInfo = disaBotInfo;

                        resultList.Add(botCommandMention);
                    }
                }
            }

            result(resultList);
        }

        // TODO: Move to TelegramUtils for both
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

        // TODO: Move to TelegramUtils for both
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

