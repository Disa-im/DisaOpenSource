using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IPartyOptionsExtended
    {
        public Task CanViewPartyBlockedParticipants(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                if (channel != null)
                {
                    if (IsAdmin(group.Address, true))
                    {
                        result(true);
                    }
                    else
                    {
                        result(false);
                    }
                }
                else
                {
                    result(false);
                }
            });
        }

        public Task CanSetPartyDescription(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                if (channel != null)
                {
                    if (IsAdmin(group.Address, true))
                    {
                        result(true);
                    }
                    else
                    {
                        result(false);
                    }
                }
                else
                {
                    result(false);
                }
            });

        }

        public Task GetPartyDescription(BubbleGroup group, Action<string> result)
        {
            return Task.Factory.StartNew(() => 
            {
                var fullChat = FetchFullChat(group.Address, true);
                var fullChannel = fullChat?.FullChat as ChannelFull;
                if (fullChannel != null)
                {
                    result(fullChannel.About);
                }
                else
                {
                    result(null);
                }
            });
        }

        public Task HasPartyDescription(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                if (channel != null)
                {
                    result(true);
                }
                else
                {
                    result(false);
                }
            });
        }

        public Task SetPartyDescription(BubbleGroup group, string description, Action<bool> result)
        {
            return Task.Factory.StartNew(() => 
            {
                if (description != null)
                {
                    using (var client = new FullClientDisposable(this)) 
                    {
                        var edited = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsEditAboutAsync(new ChannelsEditAboutArgs
                        {
                            Channel = new InputChannel
                            {
                                ChannelId = uint.Parse(group.Address),
                                AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                            },
                            About = description
                        }));
                        if (edited)
                        {
                            result(true);
                        }
                        else
                        {
                            result(false);
                        }
                    }
                }
            });
        }

        public Task GetPartyDescriptionMaxCharacters(BubbleGroup group, Action<int> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(249);
            });
        }

        public Task GetPartyDescriptionMinCharacters(BubbleGroup group, Action<int> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(0);
            });
        }

        public Task GetPartyShareLink(BubbleGroup group, Action<Tuple<string, string>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address, true);
                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                if (channel != null)
                {
                    if (channel.Username != null)
                    {
                        result(Tuple.Create(Constants.TelegramUrl, channel.Username));
                        return;
                    }
                }
                var fullChannel = fullChat?.FullChat as ChannelFull;
                if (fullChannel != null)
                {
                    var exportedInviteEmpty = fullChannel.ExportedInvite as ChatInviteEmpty;
                    if (exportedInviteEmpty != null)
                    {
                        var iExportedInviteNew = ExportChatInvite(group);
                        var exportedInviteNew = iExportedInviteNew as ChatInviteExported;
                        result(Tuple.Create<string, string>(null, exportedInviteNew.Link));
                        return;
                    }
                    var exportedInvite = fullChannel.ExportedInvite as ChatInviteExported;
                    if (exportedInvite != null)
                    {
                        result(Tuple.Create<string, string>(null, exportedInvite.Link));
                        return;
                    }
                }
                result(new Tuple<string, string>(null, null));
            });
        }

        private IExportedChatInvite ExportChatInvite(BubbleGroup group)
        {
            using (var client = new FullClientDisposable(this))
            {
                var response = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsExportInviteAsync(new ChannelsExportInviteArgs
                {
                    Channel = new InputChannel
                    {
                        ChannelId = uint.Parse(group.Address),
                        AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                    }
                }));
                return response;   
            }
        }

        public Task HasPartyShareLink(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                if (channel != null)
                {
                    if (channel.Username != null)
                    {
                        result(true);
                    }
                    else if (channel.Creator != null)
                    {
                        result(true);
                    }
                    else
                    {
                        result(false);
                    }
                }
                else
                {
                    result(false);
                }
            });
        }

        public Task GetPartyShareLinkMaxCharacters(BubbleGroup group, Action<int> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(32);
            });
        }

        public Task CanSetPartyShareLink(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address, true);
                var fullChannel = fullChat.FullChat as ChannelFull;

                if (fullChannel != null)
                {
                    bool isPublic = false;
                    var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                    if (channel != null)
                    {
                        isPublic = channel.Username != null;
                    }
                    result(isPublic && fullChannel.CanSetUsername != null);
                }
                else 
                {
                    result(false);
                }
            });
        }

        public Task GetPartyShareLinkMinCharacters(BubbleGroup group, Action<int> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(5);
            });
        }

        private bool CheckChannelUserName(BubbleGroup group, string name)
        {
            using (var client = new FullClientDisposable(this))
            {
                var response = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsCheckUsernameAsync(new ChannelsCheckUsernameArgs
                {
                    Channel = new InputChannel
                    {
                        ChannelId = uint.Parse(group.Address),
                        AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                    },
                    Username = name
                }));
                return response;
            }
        }

        private bool SetChannelUserName(BubbleGroup group, string name)
        {
            using (var client = new FullClientDisposable(this))
            {
                var response = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsUpdateUsernameAsync(new ChannelsUpdateUsernameArgs
                {
                    Channel = new InputChannel
                    {
                        ChannelId = uint.Parse(group.Address),
                        AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                    },
                    Username = name
                }));
                return response;
            }
        }

        public Task SetPartyShareLink(BubbleGroup group, Tuple<string, string> shareLink, Action<SetPartyShareLinkResult> result)
        {
            return Task.Factory.StartNew(() => 
            {
                var regex = new Regex(@"^\w+$");

                if (shareLink.Item2 != null)
                {
                    if (Char.IsDigit(shareLink.Item2, 0))
                    {
                        result(SetPartyShareLinkResult.LinkInvalid);
                    }
                    else if (shareLink.Item2[0] == '_')
                    {
                        result(SetPartyShareLinkResult.LinkInvalid);
                    }
                    else if (regex.IsMatch(shareLink.Item2))
                    {
                           //good case lets set this name
                        bool available = CheckChannelUserName(group, shareLink.Item2);
                        if (available)
                        {
                            bool changed = SetChannelUserName(group, shareLink.Item2);
                            if (changed)
                            {
                                result(SetPartyShareLinkResult.Success);
                                return;
                            }
                            result(SetPartyShareLinkResult.Failure);
                            return;
                        }
                        else
                        {
                            result(SetPartyShareLinkResult.LinkUnavailable);
                            return;
                        }

                    }
                    else 
                    {
                        result(SetPartyShareLinkResult.LinkInvalid);
                        return;
                    }
                }
                result(SetPartyShareLinkResult.Failure);
            });
        }

        public Task CanDemotePartyParticpantsFromLeader(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() => 
            {
               result(IsCreator(group.Address, group.IsExtendedParty));
            });
        }

        public Task DemotePartyParticipantsFromLeader(BubbleGroup group, DisaParticipant participant, Action<DemotePartyParticipantsResult> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var inputUser = new InputUser { UserId = uint.Parse(participant.Address) };

                using (var client = new FullClientDisposable(this))
                {
                    if (!group.IsExtendedParty)
                    {
                        if (!ChatAdminsEnabled(group.Address))
                        {
                            result(DemotePartyParticipantsResult.AllMembersAreAdministratorsEnabled);
                            return;
                        }
                        try
                        {
                            TelegramUtils.RunSynchronously(client.Client.Methods.MessagesEditChatAdminAsync(new MessagesEditChatAdminArgs
                            {
                                ChatId = uint.Parse(group.Address),
                                IsAdmin = false,
                                UserId = inputUser,
                            }));
                            result(DemotePartyParticipantsResult.Success);
                        }
                        catch (Exception e)
                        {
                            result(DemotePartyParticipantsResult.Failure);
                        }
                    }
                    else
                    {
                        try
                        {
                            var response = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsEditAdminAsync(new ChannelsEditAdminArgs
                            {
                                Channel = new InputChannel
                                {
                                    ChannelId = uint.Parse(group.Address),
                                    AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                                },
                                Role = new ChannelRoleEmpty(),
                                UserId = inputUser
                            }));
                            SendToResponseDispatcher(response, client.Client);
                            result(DemotePartyParticipantsResult.Success);
                        }
                        catch (Exception e)
                        {
                            result(DemotePartyParticipantsResult.Failure);
                        }
                    }
                }

            });
        }
    }
}

