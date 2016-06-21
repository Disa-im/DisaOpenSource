using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IPartyOptions
    {
        private MessagesChatFull _fullChat;
        object _fullChatLock = new object();

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
                var fullChat = FetchFullChat(group.Address);
                var partyParticipants = GetPartyParticipants(fullChat);
                if (!IsPartOfParty(partyParticipants))
                {
                    DebugPrint("#######  not a part of part, returning false");
                    result(false);
                    return;
                }
                if (ChatAdminsEnabled(group.Address))
                {
                    if (!IsAdmin(group.Address))
                    {
                        DebugPrint("####### Not admin returning false");
                        result(false);
                        return;
                    }
                }
                result(true);
            });
        }

        private bool IsPartOfParty(List<IChatParticipant> partyParticipants)
        {
            
            foreach(var partyParticipant in partyParticipants)
            {
                string participantAddress = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                if (participantAddress == _settings.AccountId.ToString(CultureInfo.InvariantCulture))
                {
                    return true;
                }

            }
            return false;
        }

        private List<IChatParticipant> GetPartyParticipants(MessagesChatFull fullChat)
        {
            var iChatfull = fullChat.FullChat;
            var chatFull = iChatfull as ChatFull;
            if (chatFull == null) return null;
            var chatParticipants = chatFull.Participants as ChatParticipants;
            if (chatParticipants != null)
            {
                DebugPrint("###### Party participants " + ObjectDumper.Dump(chatParticipants));
                return chatParticipants.Participants;
            }
            return null;
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
                var fullChat = FetchFullChat(group.Address);
                var partyParticipants = GetPartyParticipants(fullChat);
                if (!IsPartOfParty(partyParticipants))
                {
                    result(false);
                    return;
                }
                if (ChatAdminsEnabled(group.Address))
                {
                    if (!IsAdmin(group.Address))
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
                //Temporary to avoid the loading screen
                byte[] resizedImage = Platform.GenerateJpegBytes(bytes, 640, 640);
                var inputFile = UploadPartyImage(resizedImage);

                using (var client = new FullClientDisposable(this))
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
                    SendToResponseDispatcher(update,client.Client);
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
                    TelegramUtils.RunSynchronously(
                        client.Client.Methods.MessagesEditChatPhotoAsync(new MessagesEditChatPhotoArgs
                        {
                            ChatId = uint.Parse(group.Address),
                            Photo = new InputChatPhotoEmpty()
                        }));
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
                var fullChat = FetchFullChat(group.Address);
                var partyParticipants = GetPartyParticipants(fullChat);
                if (!IsPartOfParty(partyParticipants))
                {
                    result(false);
                    return;
                }
                if (ChatAdminsEnabled(group.Address))
                {
                    if (!IsAdmin(group.Address))
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
                    var update = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesEditChatTitleAsync(new MessagesEditChatTitleArgs
                    {
                        ChatId = uint.Parse(group.Address),
                        Title = name,
                    }));
                    SendToResponseDispatcher(update,client.Client);
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
                var fullChat = FetchFullChat(group.Address);
                var partyParticipants = GetPartyParticipants(fullChat);
                var resultList = new List<DisaParticipant>();
                foreach (var partyParticipant in partyParticipants)
                {
                    var id = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                    if (id != null)
                    {
                        var name = TelegramUtils.GetUserName(_dialogs.GetUser(uint.Parse(id)));
                        resultList.Add(new DisaParticipant(name,id));
                    }
                }
                result(resultList.ToArray());
            });
        }

        public Task CanAddPartyParticipant(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address);
                var partyParticipants = GetPartyParticipants(fullChat);
                if (!IsPartOfParty(partyParticipants))
                {
                    result(false);
                    return;
                }
                if (ChatAdminsEnabled(group.Address))
                {
                    if (!IsAdmin(group.Address))
                    {
                        result(false);
                        return;
                    }
                }
                result(true);
            });
        }

        public Task AddPartyParticipant(BubbleGroup group, DisaParticipant participant)
        {
            var inputUser = new InputUser { UserId = uint.Parse(participant.Address) };
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    var update = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesAddChatUserAsync(new MessagesAddChatUserArgs
                    {
                        UserId = inputUser,
                        ChatId = uint.Parse(group.Address),
                        FwdLimit = 0
                    }));
                    SendToResponseDispatcher(update,client.Client);
                }
            });
        }

        public Task CanDeletePartyParticipant(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address);
                var partyParticipants = GetPartyParticipants(fullChat);
                if (!IsPartOfParty(partyParticipants))
                {
                    result(false);
                    return;
                }
                if (ChatAdminsEnabled(group.Address))
                {
                    if (!IsAdmin(group.Address))
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
                var inputUser = new InputUser {UserId = uint.Parse(participant.Address)};
                using (var client = new FullClientDisposable(this))
                {
                    var update = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesDeleteChatUserAsync(new MessagesDeleteChatUserArgs
                    {
                        ChatId = uint.Parse(group.Address),
                        UserId = inputUser,
                    }));
                    SendToResponseDispatcher(update, client.Client);
                }
            });
        }

        public Task CanPromotePartyParticipantToLeader(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(IsCreator(group.Address)); 
            });
        }

        private bool IsAdmin(string address)
        {
            var fullChat = FetchFullChat(address);
            var partyParticipants = GetPartyParticipants(fullChat);
            if (!ChatAdminsEnabled(address))
            {
                return true;
            }
            foreach (var partyParticipant in partyParticipants)
            {
                var id = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                if (id == _settings.AccountId.ToString(CultureInfo.InvariantCulture))
                {
                    if ((partyParticipant is ChatParticipantAdmin)||(partyParticipant is ChatParticipantCreator))
                    {
                        return true;
                    }
                }
            }
            return false;

        }

        private bool ChatAdminsEnabled(string address)
        {
            var iChat = _dialogs.GetChat(uint.Parse(address));
            var chat = iChat as Chat;
            if (chat != null)
            {
                if (chat.AdminsEnabled != null)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        private bool IsCreator(string address)
        {
            var fullChat = FetchFullChat(address);
            var partyParticipants = GetPartyParticipants(fullChat);
            foreach (var partyParticipant in partyParticipants)
            {
                var id = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                if (id == _settings.AccountId.ToString(CultureInfo.InvariantCulture))
                {
                    //TODO:check how the protocol responds
                    if (partyParticipant is ChatParticipantCreator)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Task PromotePartyParticipantToLeader(BubbleGroup group, DisaParticipant participant)
        {
            return Task.Factory.StartNew(() =>
            {
                var inputUser = new InputUser {UserId = uint.Parse(participant.Address)};

                using (var client = new FullClientDisposable(this))
                {
                    if (!ChatAdminsEnabled(group.Address))
                    {
                        TelegramUtils.RunSynchronously(client.Client.Methods.MessagesToggleChatAdminsAsync(new MessagesToggleChatAdminsArgs
                        {
                            ChatId = uint.Parse(group.Address),
                            Enabled = true,
                        }));
                    }

                    TelegramUtils.RunSynchronously(client.Client.Methods.MessagesEditChatAdminAsync(new MessagesEditChatAdminArgs
                    {
                        ChatId = uint.Parse(group.Address),
                        IsAdmin = true,
                        UserId = inputUser,
                        
                    }));
                }
                
            });
        }

        public Task GetPartyLeaders(BubbleGroup group, Action<DisaParticipant[]> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var fullChat = FetchFullChat(group.Address);
                var partyParticipants = GetPartyParticipants(fullChat);
                List<DisaParticipant> resultList = new List<DisaParticipant>();
                if (!ChatAdminsEnabled(group.Address))
                {
                    foreach (var partyParticipant in partyParticipants)
                    {
                        var userId = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                        var user = _dialogs.GetUser(uint.Parse(userId));
                        resultList.Add(new DisaParticipant(TelegramUtils.GetUserName(user),userId));
                    }
                }
                else
                {
                    foreach (var partyParticipant in partyParticipants)
                    {
                        var userId = TelegramUtils.GetUserIdFromParticipant(partyParticipant);
                        if (partyParticipant is ChatParticipantAdmin || partyParticipant is ChatParticipantCreator)
                        {
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
            return 200;
        }

        public Task ConvertContactIdToParticipant(Contact contact,
                                           Contact.ID contactId, Action<DisaParticipant> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var telegramContact = contact as TelegramContact;
                result(new DisaParticipant(TelegramUtils.GetUserName(telegramContact.User),telegramContact.User.Id.ToString(CultureInfo.InvariantCulture)));
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
                var inputUser = new InputUser { UserId = _settings.AccountId };
                using (var client = new FullClientDisposable(this))
                {
                    var update = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesDeleteChatUserAsync(new MessagesDeleteChatUserArgs
                    {
                        ChatId = uint.Parse(group.Address),
                        UserId = inputUser,
                    }));
                    SendToResponseDispatcher(update, client.Client);
                }
            });
        }

        public Task PartyOptionsClosed()
        {
            return Task.Factory.StartNew(() =>
            {
                lock (_fullChatLock)
                {
                    _fullChat = null;
                    GC.Collect();
                }
            });
        }

        private MessagesChatFull FetchFullChat(string address)
        {
            //Classic check lock check pattern for concurrent access from all the methods
            if (_fullChat != null) return _fullChat;
            lock (_fullChatLock)
            {
                if (_fullChat != null) return _fullChat;
                using (var client = new FullClientDisposable(this))
                {
                    _fullChat =
                        (MessagesChatFull)
                            TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesGetFullChatAsync(new MessagesGetFullChatArgs
                                {
                                    ChatId = uint.Parse(address)
                                }));
                    DebugPrint("#### fullchat " + ObjectDumper.Dump(_fullChat));
                    _dialogs.AddUsers(_fullChat.Users);
                    _dialogs.AddChats(_fullChat.Chats);
                    return _fullChat;
                }
            }
        }
    }
}
