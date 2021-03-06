﻿using System;
using System.Linq;
using System.Threading.Tasks;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IPartyOptionsSettings
    {
        public Task CanConvertToExtendedParty(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                if (!group.IsExtendedParty && IsCreator(group.Address, group.IsExtendedParty))
                {
                    result(true);
                }
                else
                {
                    result(false);
                }
            });
        }

        public Task CanSetPartyAddNewMembersRestriction(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                if (group.IsExtendedParty && IsCreator(group.Address, group.IsExtendedParty))
                {
                    result(true);
                }
                else
                {
                    result(false);
                }
            });
        }

        public Task CanSetPartyAllMembersAdministratorsRestriction(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                if (!group.IsExtendedParty && IsCreator(group.Address, group.IsExtendedParty))
                {
                    result(true);
                }
                else
                {
                    result(false);
                }
            });
        }

        public Task CanSetPartyType(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                if (group.IsExtendedParty && IsCreator(group.Address, group.IsExtendedParty))
                {
                    result(true);
                }
                else
                {
                    result(false);
                }
            });
        }

        public Task ConvertToExtendedParty(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    try
                    {
                        var response = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesMigrateChatAsync(new MessagesMigrateChatArgs
                        {
                            ChatId = uint.Parse(group.Address)
                        }));
                        SendToResponseDispatcher(response, client.Client);
                        result(true);
                    }
                    catch (Exception e)
                    {
                        DebugPrint("## exception while migrating the chat " + e);
                        result(false);
                    }
                }
            });
        }

        public Task GetPartyAddNewMembersRestriction(BubbleGroup group, Action<PartyOptionsSettingsAddNewMembersRestriction> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                if (channel != null)
                {
                    if (channel.Democracy != null)
                    {
                        result(PartyOptionsSettingsAddNewMembersRestriction.Everyone);
                    }
                    else
                    {
                        result(PartyOptionsSettingsAddNewMembersRestriction.OnlyAdmins);
                    }
                }
                else
                {
                    result(PartyOptionsSettingsAddNewMembersRestriction.Unknown);
                }
            });
        }

        public Task GetPartyAllMembersAdmininistratorsRestriction(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                if (ChatAdminsEnabled(group.Address))
                {
                    result(true);
                }
                else
                {
                    result(false);
                }
            });
        }

        public Task GetPartyType(BubbleGroup group, Action<PartyOptionsSettingsPartyType> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                if (channel != null)
                {
                    if (!string.IsNullOrEmpty(channel.Username))
                    {
                        result(PartyOptionsSettingsPartyType.Public);
                    }
                    else
                    {
                        result(PartyOptionsSettingsPartyType.Private);
                    }
                }
                else
                {
                    result(PartyOptionsSettingsPartyType.Unknown);
                }
            });
        }

        public Task SetPartyAddNewMembersRestriction(BubbleGroup group, PartyOptionsSettingsAddNewMembersRestriction restriction, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    try
                    {
                        var response = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsToggleInvitesAsync(new ChannelsToggleInvitesArgs
                        {
                            Channel = new InputChannel
                            {
                                ChannelId = uint.Parse(group.Address),
                                AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                            },
                            Enabled = restriction == PartyOptionsSettingsAddNewMembersRestriction.Everyone
                        }));
                        SendToResponseDispatcher(response, client.Client);
                        result(true);
                    }
                    catch (Exception e)
                    {
                        DebugPrint("##### Exeption while setting all members are admins type " + e);
                        result(false);
                    }
                }
            });
        }

        public Task SetPartyAllMembersAdmininistratorsRestriction(BubbleGroup group, bool restriction, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    try
                    {
                        var response = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesToggleChatAdminsAsync(new SharpTelegram.Schema.MessagesToggleChatAdminsArgs
                        {
                            ChatId = uint.Parse(group.Address),
                            Enabled = !restriction //enable chat admins if all memebers are not adminstrators
                        }));
                        SendToResponseDispatcher(response, client.Client);
                        result(true);
                    }
                    catch (Exception e)
                    {
                        DebugPrint("##### Exeption while setting all members are admins type " + e);
                        result(false);
                    }
                }
            });
        }

        public Task SetPartyType(BubbleGroup group, PartyOptionsSettingsPartyType type, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    if (type == PartyOptionsSettingsPartyType.Public)
                    {
                        var randomUserName = RandomString(32);
                        DebugPrint("The random username is " + randomUserName);
                        using (var client = new FullClientDisposable(this))
                        {
                            var response = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsUpdateUsernameAsync(new SharpTelegram.Schema.ChannelsUpdateUsernameArgs
                            {
                                Channel = new InputChannel
                                {
                                    ChannelId = uint.Parse(group.Address),
                                    AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                                },
                                Username = randomUserName
                            }));
                            result(response);
                        }
                    }
                    else
                    {
                        using (var client = new FullClientDisposable(this))
                        {
                            var response = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsUpdateUsernameAsync(new SharpTelegram.Schema.ChannelsUpdateUsernameArgs
                            {
                                Channel = new InputChannel
                                {
                                    ChannelId = uint.Parse(group.Address),
                                    AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                                },
                                Username = ""
                            }));
                            result(response);
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugPrint("##### Exeption while setting group type " + e);
                    result(false);
                }
            });
        }

        public Task CanSignMessages(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Are we a Telegram Channel?
                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                if (channel == null)
                {
                    // Ok, we are either a Disa Solo or Disa Pary group
                    // so signing is not available
                    result(false);
                }
                else
                {
                    // Ok, we are either a Disa Super or Disa Channel group at this point,
                    // so:
                    var canSignMessages = channel.Broadcast != null &&       // Are we are a Disa Channel
                                          (channel.Creator == null ||        // AND we are the creator
                                           channel.Editor == null);          // OR we an editor?
                    result(canSignMessages);
                }
            });
        }

        public Task GetSignMessages(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Are we a Telegram Channel?
                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                if (channel == null)
                {
                    // Ok, we are either a Disa Solo or Disa Pary group
                    // so signing is not available
                    result(false);
                }
                else
                {
                    // Ok, we are either a Disa Super or Disa Channel group at this point,
                    // so:
                    var signMessages =  channel.Broadcast != null &&       // Are we are a Disa Channel
                                        channel.Signatures != null;        // AND we group has signing on?
                    result(signMessages);
                }
            });
        }

        public Task SignMessages(BubbleGroup group, bool sign, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    try
                    {
                        var response = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsToggleSignaturesAsync(new ChannelsToggleSignaturesArgs
                        {
                            Channel = new InputChannel
                            {
                                ChannelId = uint.Parse(group.Address),
                                AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                            },
                            Enabled = sign
                        }));
                        SendToResponseDispatcher(response, client.Client);
                        result(true);
                    }
                    catch (Exception e)
                    {
                        DebugPrint("## exception while migrating the chat " + e);
                        result(false);
                    }
                }
            });

        }

        private string RandomString(int length)
        {
            var random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

