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
        public Task GetTokens(Action<List<string>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(new List<string>(new string[] { "@", "#", @"/" }));
            });
        }

        // Usernames - we need to return usernames and names for a particular group
        // Hasthags - we need to return most recent hashtags across ALL groups
        // BotCommand - we need to return a bot usernames and commands for a particular group
        public Task GetMentions(string token, BubbleGroup group, Action<List<Mentions>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Only handling usernames now
                if (token != "@")
                {
                    result(new List<Mentions>());
                }

                var fullChat = FetchFullChat(group.Address, group.IsExtendedParty);

                // Get fresh list of participants, not the cache
                DisposeFullChat();
                var partyParticipants = GetPartyParticipants(fullChat);

                var resultList = new List<Mentions>();
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
                            var mention  = new Username
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
                            var mention = new Username
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
    }
}

