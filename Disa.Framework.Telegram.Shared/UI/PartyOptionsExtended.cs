using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IPartyOptionsExtended
    {
        
        public Task CanSetPartyDescription(BubbleGroup group, Action<bool> result)
        {
            throw new NotImplementedException();
        }

        public Task CanViewPartyBlockedParticipants(BubbleGroup group, Action<bool> result)
        {
            throw new NotImplementedException();
        }

        public Task GetPartyDescription(BubbleGroup group, Action<string> result)
        {
            throw new NotImplementedException();
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

        public Task HasPartyDescription(BubbleGroup group, Action<bool> result)
        {
            throw new NotImplementedException();
        }

        public Task SetPartyDescription(BubbleGroup group, string description, Action<bool> result)
        {
            throw new NotImplementedException();
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

        public Task SetPartyShareLink(BubbleGroup group, Tuple<string, string> shareLink, Action<PartyShareLinkResult> result)
        {
            return Task.Factory.StartNew(() => 
            {
                var regex = new Regex(@"^\w+$");

                if (shareLink.Item2 != null)
                {
                    if (Char.IsDigit(shareLink.Item2, 0))
                    {
                        result(PartyShareLinkResult.LinkInvalid);
                    }
                    else if (shareLink.Item2[0] == '_')
                    {
                        result(PartyShareLinkResult.LinkInvalid);
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
                                result(PartyShareLinkResult.Success);
                                return;
                            }
                            result(PartyShareLinkResult.Failure);
                            return;
                        }
                        else
                        {
                            result(PartyShareLinkResult.LinkUnavailable);
                            return;
                        }

                    }
                    else 
                    {
                        result(PartyShareLinkResult.LinkInvalid);
                        return;
                    }
                }
                result(PartyShareLinkResult.Failure);
            });
        }

   }
}

