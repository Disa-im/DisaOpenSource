using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IPartyBlockedParticipants
    {
        public Task CanPartyUnblockParticipants(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        public Task GetPartyBlockedParticipantPicture(BubbleGroup group, string address, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(GetThumbnail(address, false, true));
            });
        }

        public Task GetPartyBlockedParticipants(BubbleGroup group, Action<DisaParticipant[]> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var returnList = new List<DisaParticipant>();
                var fullChat = FetchFullChat(group.Address, group.IsExtendedParty);
                var iChatFull = fullChat.FullChat;
                var channelFull = iChatFull as ChannelFull;
                if (channelFull != null)
                {
                    var kickedParticipants =  GetChannelParticipants(channelFull, new ChannelParticipantsKicked());
                    foreach (var participant in kickedParticipants) 
                    {
                        if (participant is ChannelParticipantKicked)
                        {
                            var id = TelegramUtils.GetUserIdFromChannelParticipant(participant);
                            if (id != null)
                            {
                                var user = _dialogs.GetUser(uint.Parse(id));
                                var name = TelegramUtils.GetUserName(user);
                                returnList.Add(new DisaParticipant(name, id));
                            }
                        }
                    }
                }
                result(returnList.ToArray());
            });
        }

        public Task UnblockPartyParticipant(BubbleGroup group, DisaParticipant participant, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    try
                    {
                        var response = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsKickFromChannelAsync(new ChannelsKickFromChannelArgs
                        {
                            Channel = new InputChannel
                            {
                                ChannelId = uint.Parse(group.Address),
                                AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                            },
                            Kicked = false,
                            UserId = new InputUser
                            {
                                UserId = uint.Parse(participant.Address),
                                AccessHash = GetUserAccessHashIfForeign(participant.Address)
                            }
                        }));
                        result(true);
                    }
                    catch (Exception e)
                    {
                        DebugPrint("#### Exception " + e);
                        result(false);
                    }
                }
            });
        }
    }
}

