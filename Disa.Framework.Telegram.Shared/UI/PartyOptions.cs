using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using SharpTelegram.Schema;
using System.Linq;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IPartyOptions
    {
        private MessagesChatFull _fullChat;
        private Participants _participants;
        object _fullChatLock = new object();
        object _participantsLock = new object();

        private enum ParticipantsType 
        {
            Chat,
            Channel
        };

        private class Participants
        {
            public ParticipantsType Type { get; set; }
            public List<IChatParticipant> ChatParticipants { get; set; }
            public List<IChannelParticipant> ChannelParticipants { get; set; }
        }

        private class ChannelParticipantComparer : IEqualityComparer<IChannelParticipant>
        {
            public bool Equals(IChannelParticipant x, IChannelParticipant y)
            {
                var userIdX = TelegramUtils.GetUserIdFromChannelParticipant(x);
                var userIdY = TelegramUtils.GetUserIdFromChannelParticipant(y);
                return userIdX == userIdY;
            }

            public int GetHashCode(IChannelParticipant obj)
            {
                return int.Parse(TelegramUtils.GetUserIdFromChannelParticipant(obj));
            }
        }


        public Task GetPartyPhoto(BubbleGroup group, DisaParticipant participant, bool preview, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                DisaThumbnail thumbnail;
                if (participant == null)
                {
                    thumbnail = GetThumbnail(group.Address, group.IsParty, preview);
                }
                else
                {
                    thumbnail = GetThumbnail(participant.Address, false, preview);
                }
                result(thumbnail);
            });
        }

        public Task CanSetPartyPhoto(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address, group.IsExtendedParty);
                var partyParticipants = GetPartyParticipants(fullChat);
                if (!IsPartOfParty(partyParticipants))
                {
                    DebugPrint("#######  not a part of part, returning false");
                    result(false);
                    return;
                }
                if (ChatAdminsEnabled(group.Address))
                {
                    if (!IsAdmin(group.Address, group.IsExtendedParty))
                    {
                        DebugPrint("####### Not admin returning false");
                        result(false);
                        return;
                    }
                }
                result(true);
            });
        }

        private bool IsPartOfParty(Participants partyParticipants)
        {
            if (partyParticipants.Type == ParticipantsType.Chat)
            {
                foreach (var partyParticipant in partyParticipants.ChatParticipants)
                {
                    string participantAddress = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                    if (participantAddress == Settings.AccountId.ToString(CultureInfo.InvariantCulture))
                    {
                        return true;
                    }
                }
            }
            else 
            {
                foreach (var partyParticipant in partyParticipants.ChannelParticipants)
                {
                    string participantAddress = TelegramUtils.GetUserIdFromChannelParticipant(partyParticipant);
                    if (participantAddress == Settings.AccountId.ToString(CultureInfo.InvariantCulture))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private Participants GetPartyParticipants(MessagesChatFull fullChat)
        {
            if (_participants != null)
            {
                return _participants;
            }
            lock (_participantsLock)
            {
                if (_participants != null)
                {
                    return _participants;
                }
                var iChatFull = fullChat.FullChat;
                var chatFull = iChatFull as ChatFull;
                var channelFull = iChatFull as ChannelFull;
                if (chatFull != null)
                {
                    var chatParticipants = chatFull.Participants as ChatParticipants;
                    if (chatParticipants != null)
                    {
                        DebugPrint("###### Party participants " + ObjectDumper.Dump(chatParticipants));
                        _participants = new Participants
                        {
                            Type = ParticipantsType.Chat,
                            ChatParticipants = chatParticipants.Participants
                        };
                        return _participants;
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
                    DebugPrint("###### Party participants " + ObjectDumper.Dump(channelAdmins));
                    _participants = new Participants
                    {
                        Type = ParticipantsType.Channel,
                        ChannelParticipants = mergedList
                    };
                    return _participants;
                }
            }
            return null;
        }

        private List<IChannelParticipant> GetChannelParticipants(ChannelFull channelFull, IChannelParticipantsFilter filter)
        {
            var participantsList = new List<IChannelParticipant>();
            using (var client = new FullClientDisposable(this)) 
            {
                uint count = 100;
                uint offset = 0;
                var result = (ChannelsChannelParticipants)TelegramUtils.RunSynchronously(
                    client.Client.Methods.ChannelsGetParticipantsAsync(new ChannelsGetParticipantsArgs
                    {
                        Channel = new InputChannel
                        {
                            ChannelId = channelFull.Id,
                            AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(channelFull.Id))
                        },
                        Filter = filter,
                        Limit = 100,
                        Offset = offset
                    }));
                participantsList.AddRange(result.Participants);
                _dialogs.AddUsers(result.Users);

            }
            return participantsList;
        }

        public Task CanViewPartyPhoto(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        public Task CanDeletePartyPhoto(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address, group.IsExtendedParty);
                var partyParticipants = GetPartyParticipants(fullChat);
                if (!IsPartOfParty(partyParticipants))
                {
                    result(false);
                    return;
                }
                if (ChatAdminsEnabled(group.Address))
                {
                    if (!IsAdmin(group.Address, group.IsExtendedParty))
                    {
                        result(false);
                        return;
                    }
                }
                result(true);
            });
        }

        public Task SetPartyPhoto(BubbleGroup group, byte[] bytes, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                //MessagesEditChatPhotoAsync
                byte[] resizedImage = Platform.GenerateJpegBytes(bytes, 640, 640);
                var inputFile = UploadPartyImage(resizedImage);

                using (var client = new FullClientDisposable(this))
                {
                    if (!group.IsExtendedParty)
                    {
                        var update = TelegramUtils.RunSynchronously(
                            client.Client.Methods.MessagesEditChatPhotoAsync(new MessagesEditChatPhotoArgs
                            {
                                ChatId = uint.Parse(group.Address),
                                Photo = new InputChatUploadedPhoto
                                {
                                    Crop = new InputPhotoCropAuto(),
                                    File = inputFile
                                }
                            }));
                        SendToResponseDispatcher(update, client.Client);
                    }
                    else 
                    {
                        //ChannelsEditPhoto
                        var update = TelegramUtils.RunSynchronously(
                            client.Client.Methods.ChannelsEditPhotoAsync(new ChannelsEditPhotoArgs 
                        { 
                            Channel = new InputChannel
                            {
                                ChannelId = uint.Parse(group.Address),
                                AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                            },
                            Photo = new InputChatUploadedPhoto
                            {
                                Crop = new InputPhotoCropAuto(),
                                File = inputFile
                            }
                        }));
                        SendToResponseDispatcher(update, client.Client);
                    }
                }
                byte[] disaImage = Platform.GenerateJpegBytes(bytes, 96, 96);
                var thumbnail = new DisaThumbnail(this,disaImage,GenerateRandomId().ToString());
                result(thumbnail);
            });
        }

        private IInputFile UploadPartyImage(byte[] resizedImage)
        {
            var fileId = GenerateRandomId();
            const int chunkSize = 65536;
            var chunk = new byte[chunkSize];
            uint chunkNumber = 0;
            var offset = 0;
            using (var memoryStream = new MemoryStream(resizedImage))
            {
                using (var client = new FullClientDisposable(this))
                {
                    int bytesRead;
                    while ((bytesRead = memoryStream.Read(chunk, 0, chunk.Length)) > 0)
                    {
                        //RPC call

                        var uploaded =
                            TelegramUtils.RunSynchronously(
                                client.Client.Methods.UploadSaveFilePartAsync(new UploadSaveFilePartArgs
                                {
                                    Bytes = chunk,
                                    FileId = fileId,
                                    FilePart = chunkNumber
                                }));

                        if (!uploaded)
                        {
                            return null;
                        }
                        chunkNumber++;
                        offset += bytesRead;
                    }
                    return new InputFile
                    {
                        Id = fileId,
                        Md5Checksum = "",
                        Name = GenerateRandomId() + ".jpeg",
                        Parts = chunkNumber
                    };
                }
            }

        }

        public Task DeletePartyPhoto(BubbleGroup group)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    if (!group.IsExtendedParty)
                    {
                        var update = TelegramUtils.RunSynchronously(
                            client.Client.Methods.MessagesEditChatPhotoAsync(new MessagesEditChatPhotoArgs
                            {
                                ChatId = uint.Parse(group.Address),
                                Photo = new InputChatPhotoEmpty()
                            }));
                        SendToResponseDispatcher(update, client.Client);
                    }
                    else
                    {
                        var update = TelegramUtils.RunSynchronously(
                            client.Client.Methods.ChannelsEditPhotoAsync(new ChannelsEditPhotoArgs
                            {
                                Channel = new InputChannel
                                {
                                    ChannelId = uint.Parse(group.Address),
                                    AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                                },
                                Photo = new InputChatPhotoEmpty()
                            }));
                        SendToResponseDispatcher(update, client.Client);
                    }
                }
            });
        }

        public Task GetPartyName(BubbleGroup group, Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(TelegramUtils.GetChatTitle(_dialogs.GetChat(uint.Parse(group.Address))));
            });
        }

        public Task CanSetPartyName(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address, group.IsExtendedParty);
                var partyParticipants = GetPartyParticipants(fullChat);
                if (!IsPartOfParty(partyParticipants))
                {
                    result(false);
                    return;
                }
                if (ChatAdminsEnabled(group.Address))
                {
                    if (!IsAdmin(group.Address, group.IsExtendedParty))
                    {
                        result(false);
                        return;
                    }
                }
                result(true);
            });
        }

        public Task SetPartyName(BubbleGroup group, string name)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    if (!group.IsExtendedParty)
                    {
                        var update = TelegramUtils.RunSynchronously(
                            client.Client.Methods.MessagesEditChatTitleAsync(new MessagesEditChatTitleArgs
                        {
                            ChatId = uint.Parse(group.Address),
                            Title = name,
                        }));
                        SendToResponseDispatcher(update, client.Client);
                    }
                    else 
                    { 
                        var update = TelegramUtils.RunSynchronously(
                            client.Client.Methods.ChannelsEditTitleAsync(new ChannelsEditTitleArgs 
                        { 
                            Channel = new InputChannel
                            {
                                ChannelId = uint.Parse(group.Address),
                                AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                            },
                            Title = name
                        }));
                        SendToResponseDispatcher(update, client.Client);
                    }
                }

            });
        }

        public Task GetPartyNameMaxLength(BubbleGroup group, Action<int> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(249);//calculated using a sophesticated algorithm, i.e trying the maximum letters offical telegram client can set
            });
        }

        public Task GetPartyParticipants(BubbleGroup group, Action<DisaParticipant[]> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address, group.IsExtendedParty);
                var partyParticipants = GetPartyParticipants(fullChat);
                var resultList = new List<DisaParticipant>();
                if (!group.IsExtendedParty)
                {
                    foreach (var partyParticipant in partyParticipants.ChatParticipants)
                    {
                        var id = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                        if (id != null)
                        {
                            var name = TelegramUtils.GetUserName(_dialogs.GetUser(uint.Parse(id)));
                            resultList.Add(new DisaParticipant(name, id));
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
                            var name = TelegramUtils.GetUserName(_dialogs.GetUser(uint.Parse(id)));
                            resultList.Add(new DisaParticipant(name, id));
                        }
                    }
                
                }
                result(resultList.ToArray());
            });
        }

        public Task CanAddPartyParticipant(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address, group.IsExtendedParty);
                var partyParticipants = GetPartyParticipants(fullChat);
                if (!IsPartOfParty(partyParticipants))
                {
                    result(false);
                    return;
                }
                if (ChatAdminsEnabled(group.Address))
                {
                    if (group.IsExtendedParty && IsDemocracyEnabled(group))
                    {
                        //if democracy is enabled in a spergroup anyone can add members
                        result(true);
                        return;
                    }
                    if (!IsAdmin(group.Address, group.IsExtendedParty))
                    {
                        result(false);
                        return;
                    }
                }
                result(true);
            });
        }

        private bool IsDemocracyEnabled(BubbleGroup group)
        {
            var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
            if (channel != null)
            {
                return channel.Democracy != null;
            }
            return false;
        }

        public Task AddPartyParticipant(BubbleGroup group, DisaParticipant participant)
        {
            var inputUser = new InputUser 
            { 
                UserId = uint.Parse(participant.Address), 
                AccessHash = GetUserAccessHashIfForeign(participant.Address)
            };
            return Task.Factory.StartNew(() =>
            {
				try
				{
					using (var client = new FullClientDisposable(this))
					{
						if (!group.IsExtendedParty)
						{
							var update = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesAddChatUserAsync(new MessagesAddChatUserArgs
							{
								UserId = inputUser,
								ChatId = uint.Parse(group.Address),
								FwdLimit = 0
							}));
							SendToResponseDispatcher(update, client.Client);
						}
						else
						{
							var update = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsInviteToChannelAsync(new ChannelsInviteToChannelArgs
							{
								Channel = new InputChannel
								{
									ChannelId = uint.Parse(group.Address),
									AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
								},
								Users = new List<IInputUser>
							{
								inputUser
							}
							}));
							SendToResponseDispatcher(update, client.Client);

						}
					}
				}
				catch (Exception e)
				{
					Utils.DebugPrint("Exception while adding user to the group " + e);
				}
            });
        }

        public Task CanDeletePartyParticipant(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address, group.IsExtendedParty);
                var partyParticipants = GetPartyParticipants(fullChat);
                if (!IsPartOfParty(partyParticipants))
                {
                    result(false);
                    return;
                }
                if (ChatAdminsEnabled(group.Address))
                {
                    if (!IsAdmin(group.Address, group.IsExtendedParty))
                    {
                        result(false);
                        return;
                    }
                }
                result(true);
            });
        }

        public Task DeletePartyParticipant(BubbleGroup group, DisaParticipant participant)
        {
            return Task.Factory.StartNew(() =>
            {
                var inputUser = new InputUser
                {
                    UserId = uint.Parse(participant.Address),
                    AccessHash = GetUserAccessHashIfForeign(participant.Address)
                };
                using (var client = new FullClientDisposable(this))
                {
                    if (!group.IsExtendedParty)
                    {
                        var update = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesDeleteChatUserAsync(new MessagesDeleteChatUserArgs
                        {
                            ChatId = uint.Parse(group.Address),
                            UserId = inputUser,
                        }));
                        SendToResponseDispatcher(update, client.Client);
                    }
                    else
                    {
                        try
                        {
                            var update = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsKickFromChannelAsync(new ChannelsKickFromChannelArgs
                            {
                                Channel = new InputChannel
                                {
                                    ChannelId = uint.Parse(group.Address),
                                    AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                                },
                                Kicked = true,
                                UserId = inputUser
                            }));
                            SendToResponseDispatcher(update, client.Client);
                        }
                        catch (Exception e)
                        {
                            DebugPrint("Exception " + e);
                        }
                    }
                }
            });
        }

        public Task CanPromotePartyParticipantToLeader(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(IsCreator(group.Address, group.IsExtendedParty)); 
            });
        }

        private bool IsAdmin(string address, bool isSuperGroup)
        {
            var fullChat = FetchFullChat(address, isSuperGroup);
            var partyParticipants = GetPartyParticipants(fullChat);

            if (!isSuperGroup)
            {
                if (!ChatAdminsEnabled(address))
                {
                    return true;
                }
                foreach (var partyParticipant in partyParticipants.ChatParticipants)
                {
                    var id = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                    if (id == Settings.AccountId.ToString(CultureInfo.InvariantCulture))
                    {
                        if ((partyParticipant is ChatParticipantAdmin) || (partyParticipant is ChatParticipantCreator))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                foreach (var partyParticipant in partyParticipants.ChannelParticipants)
                {
                    var id = TelegramUtils.GetUserIdFromChannelParticipant(partyParticipant);
                    if (id == Settings.AccountId.ToString(CultureInfo.InvariantCulture))
                    {
                        if ((partyParticipant is ChannelParticipantCreator) || (partyParticipant is ChannelParticipantEditor)
                            || (partyParticipant is ChannelParticipantModerator))
                        {
                            return true;
                        }
                    }
                }
                
            }
            return false;

        }

        private bool ChatAdminsEnabled(string address)
        {
            var iChat = _dialogs.GetChat(uint.Parse(address));
            var chat = iChat as Chat;
            var channel = iChat as Channel;
            if (chat != null)
            {
                if (chat.AdminsEnabled != null)
                {
                    return true;
                }
                return false;
            }
            if (channel != null)
            {
                return true;
            }
            return false;
        }

        private bool IsCreator(string address, bool superGroup)
        {
            var fullChat = FetchFullChat(address, superGroup);
            var partyParticipants = GetPartyParticipants(fullChat);
            if (!superGroup)
            {
                foreach (var partyParticipant in partyParticipants.ChatParticipants)
                {
                    var id = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                    if (id == Settings.AccountId.ToString(CultureInfo.InvariantCulture))
                    {
                        //TODO:check how the protocol responds
                        if (partyParticipant is ChatParticipantCreator)
                        {
                            return true;
                        }
                    }
                }
            }
            else 
            {
                var channel = _dialogs.GetChat(uint.Parse(address)) as Channel;
                if (channel != null)
                {
                    return channel.Creator != null;
                }
            }
            return false;
        }

        public Task PromotePartyParticipantToLeader(BubbleGroup group, DisaParticipant participant)
        {
            return Task.Factory.StartNew(() =>
            {
                var inputUser = new InputUser 
                {
                    UserId = uint.Parse(participant.Address),
                    AccessHash = GetUserAccessHashIfForeign(participant.Address)
                };

                using (var client = new FullClientDisposable(this))
                {
                    if (!ChatAdminsEnabled(group.Address))
                    {
                        //this condition should ideally never be hit
                        // if chat admins are disabled, everyone is an admin and hence the ui never allows anyone to be promoted to an admin
                    }

                    if (!group.IsExtendedParty)
                    {
                        TelegramUtils.RunSynchronously(client.Client.Methods.MessagesEditChatAdminAsync(new MessagesEditChatAdminArgs
                        {
                            ChatId = uint.Parse(group.Address),
                            IsAdmin = true,
                            UserId = inputUser,
                        }));
                    }
                    else
                    {
                        TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsEditAdminAsync(new ChannelsEditAdminArgs
                        {
                            Channel = new InputChannel
                            {
                                ChannelId = uint.Parse(group.Address),
                                AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                            },
                            Role = new ChannelRoleModerator(),
                            UserId = inputUser
                        }));
                        
                    }
                }
                
            });
        }

        public Task GetPartyLeaders(BubbleGroup group, Action<DisaParticipant[]> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address, group.IsExtendedParty);
                var partyParticipants = GetPartyParticipants(fullChat);
                List<DisaParticipant> resultList = new List<DisaParticipant>();

                if (!group.IsExtendedParty)
                {
                    if (!ChatAdminsEnabled(group.Address))
                    {
                        foreach (var partyParticipant in partyParticipants.ChatParticipants)
                        {
                            var userId = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                            var user = _dialogs.GetUser(uint.Parse(userId));
                            resultList.Add(new DisaParticipant(TelegramUtils.GetUserName(user), userId));
                        }
                    }
                    else
                    {
                        foreach (var partyParticipant in partyParticipants.ChatParticipants)
                        {
                            var userId = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                            if (partyParticipant is ChatParticipantAdmin || partyParticipant is ChatParticipantCreator)
                            {
                                var user = _dialogs.GetUser(uint.Parse(userId));
                                resultList.Add(new DisaParticipant(TelegramUtils.GetUserName(user), userId));
                            }
                        }
                    }
                }
                else
                {
                    foreach (var partyParticipant in partyParticipants.ChannelParticipants)
                    {
                        if ((partyParticipant is ChannelParticipantCreator) || (partyParticipant is ChannelParticipantEditor)
                           || (partyParticipant is ChannelParticipantModerator))
                        {
                            var userId = TelegramUtils.GetUserIdFromChannelParticipant(partyParticipant);
                            var user = _dialogs.GetUser(uint.Parse(userId));
                            resultList.Add(new DisaParticipant(TelegramUtils.GetUserName(user), userId));
                        }
                    }
                }
                result(resultList.ToArray());
            });
        }

        public int GetMaxParticipantsAllowed()
        {
            return 5000;
        }

        public Task ConvertContactIdToParticipant(Contact contact,
                                           Contact.ID contactId, Action<DisaParticipant> result)
        {
            return Task.Factory.StartNew(() =>
            {
                User user = null;
                if (contact is TelegramContact)
                { 
                    user = (contact as TelegramContact).User;
                }
                else if(contact is TelegramBotContact)
                {
                    user = (contact as TelegramBotContact).User;
                }
                result(new DisaParticipant(TelegramUtils.GetUserName(user),user.Id.ToString(CultureInfo.InvariantCulture)));
            });
        }

        public Task CanLeaveParty(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        public Task LeaveParty(BubbleGroup group)
        {
            return Task.Factory.StartNew(() =>
            {
                var inputUser = new InputUser { UserId = Settings.AccountId };
                using (var client = new FullClientDisposable(this))
                {
                    if (!group.IsExtendedParty)
                    {
                        var update = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesDeleteChatUserAsync(new MessagesDeleteChatUserArgs
                        {
                            ChatId = uint.Parse(group.Address),
                            UserId = inputUser,
                        }));
                        SendToResponseDispatcher(update, client.Client);
                    }
                    else
                    { 
                        var update = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsLeaveChannelAsync(new ChannelsLeaveChannelArgs
                        { 
                            Channel = new InputChannel
                            {
                                ChannelId = uint.Parse(group.Address),
                                AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                            }
                        }));
                        SendToResponseDispatcher(update, client.Client);
                    }
                }
            });
        }

        public Task PartyOptionsClosed()
        {
            return Task.Factory.StartNew(() =>
            {
                DisposeFullChat();
            });
        }

        private void DisposeFullChat()
        {
            lock (_fullChatLock)
            {
                _fullChat = null;
                GC.Collect();
            }
            lock (_participantsLock)
            {
                _participants = null;
                GC.Collect();
            }
        }

        private MessagesChatFull FetchFullChat(string address, bool superGroup)
        {
            //Classic check lock check pattern for concurrent access from all the methods
            if (_fullChat != null) return _fullChat;
            lock (_fullChatLock)
            {
                if (_fullChat != null) return _fullChat;
                using (var client = new FullClientDisposable(this))
                {
                    if (!superGroup)
                    {
                        _fullChat =
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
                            _fullChat =
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
                    //DebugPrint("#### fullchat " + ObjectDumper.Dump(_fullChat));
                    _dialogs.AddUsers(_fullChat.Users);
                    _dialogs.AddChats(_fullChat.Chats);
                    return _fullChat;
                }
            }
        }
    }
}
