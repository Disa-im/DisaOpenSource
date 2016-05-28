using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SharpTelegram.Schema;
using System.Linq;
using System.Globalization;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : INewMessage
    {
        public Task GetContacts(string query, bool searchForParties, Action<List<Contact>> result)
        {
            return Task.Factory.StartNew(async () =>
            {
                if (!searchForParties)
                {
                    var users = await FetchContacts();
                    var contacts = users.Select(x => 
                        new TelegramContact 
                    {
                        Available = TelegramUtils.GetAvailable(x),
                        LastSeen = TelegramUtils.GetLastSeenTime(x),
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        Ids = new List<Contact.ID> 
                        { 
                            new Contact.ID 
                            { 
                                Service = this, 
                                Id = x.Id.ToString(CultureInfo.InvariantCulture)
                            } 
                        },
                    }).OfType<Contact>().OrderBy(x => x.FirstName).ToList();
                    result(contacts);
                }
                else
                {
                    var partyContacts = new List<Contact>();
                    //TODO: manually call upon GetDialogs method call to get the latest dialogs.
                    foreach (var iDialog in _dialogs.Dialogs)
                    {
                        var dialog = iDialog as Dialog;
                        if (dialog == null)
                            continue;
                        var peerChat = dialog.Peer as PeerChat;
                        if (peerChat == null)
                            continue;
                        var peerChatId = peerChat.ChatId.ToString(CultureInfo.InvariantCulture);
                        string name = null;
                        foreach (var iChat in _dialogs.Chats)
                        {
                            var chatId = TelegramUtils.GetChatId(iChat);
                            if (chatId == peerChatId)
                            {
                                name = TelegramUtils.GetChatTitle(iChat);
                                break;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            partyContacts.Add(new TelegramPartyContact
                            {
                                FirstName = name,
                                Ids = new List<Contact.ID>
                                {
                                    new Contact.PartyID
                                    {
                                        Service = this,
                                        Id = peerChatId,
                                    }
                                },
                            });
                        }
                    }
                    result(partyContacts);
                }
            });
        }

        public Task GetContactsFavorites(Action<List<Contact>> result)
        {
            return Task.Factory.StartNew(async () =>
            {
                var count = 0;
                var inputUsers = new List<IInputUser>();
                foreach (var bubbleGroup in BubbleGroupManager.SortByMostPopular(this, true))
                {
                    var address = bubbleGroup.Address;
                    foreach (var user in _dialogs.Users)
                    {
                        var userId = TelegramUtils.GetUserId(user);
                        if (userId == address)
                        {
                            var inputUser = TelegramUtils.CastUserToInputUser(user);
                            if (inputUser != null)
                            {
                                inputUsers.Add(inputUser);
                                break;
                            }
                        }
                    }
                    count++;
                    if (count > 6)
                        break;
                }
                if (!inputUsers.Any())
                {
                    result(null);
                }
                else
                {
                    using (var client = new FullClientDisposable(this))
                    {
                        var users = await GetUsers(inputUsers, client.Client);
                        var contacts = users.Select(x => 
                            new TelegramContact 
                        {
                            Available = TelegramUtils.GetAvailable(x),
                            LastSeen =  TelegramUtils.GetLastSeenTime(x),
                            FirstName = TelegramUtils.GetUserName(x),
                            Ids = new List<Contact.ID> 
                            { 
                                new Contact.ID 
                                { 
                                    Service = this, 
                                    Id = TelegramUtils.GetUserId(x).ToString(CultureInfo.InvariantCulture)
                                } 
                            },
                        }).OfType<Contact>().OrderBy(x => x.FirstName).ToList();
                        result(contacts);
                    }
                }
            });
        }

        public Task GetContactPhoto(Contact contact, bool preview, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(GetThumbnail(contact.Ids[0].Id, contact is TelegramPartyContact, preview));
            });
        }

        public Task FetchBubbleGroup(Contact.ID[] contactIds, Action<BubbleGroup> result)
        {
            return Task.Factory.StartNew(() =>
            {
                foreach (var group in BubbleGroupManager.FindAll(this))
                {
                    if (contactIds.Length <= 1)
                    {
                        foreach (var contactId in contactIds)
                        {
                            if (BubbleGroupComparer(contactId.Id, group.Address))
                            {
                                result(group);
                                return;
                            }
                        }
                    }
                }
                    
                result(null);
            });
        }

        public Task FetchBubbleGroupAddress(Tuple<Contact, Contact.ID>[] contacts, Action<bool, string> result)
        {
            return Task.Factory.StartNew(async () =>
            {
                if (contacts.Length > 1)
                {
                    var userContacts = await FetchContacts();
                    var inputUsers = new List<IInputUser>();
                    var names = new List<string>();
                    foreach (var contact in contacts)
                    {
                        names.Add(contact.Item1.FullName);
                        var id = uint.Parse(contact.Item2.Id);
                        foreach (var userContact in userContacts)
                        {
                            if (userContact.Id == id)
                            {
                                var inputUser = TelegramUtils.CastUserToInputUser(userContact);
                                if (inputUser != null)
                                {
                                    inputUsers.Add(inputUser);
                                    break;
                                }
                            }
                        }
                    }
                    if (inputUsers.Any())
                    {
                        var subject = BubbleGroupUtils.GeneratePartyTitle(names.ToArray());
                        if (subject.Length > 25)
                        {
                            subject = subject.Substring(0, 22);
                            subject += "...";
                        }
                        using (var client = new FullClientDisposable(this))
                        {
                            var response = await client.Client.Methods.MessagesCreateChatAsync(
                                new MessagesCreateChatArgs
                            {
                                Users = inputUsers,
                                Title = subject,
                            });
//                            ProcessIncomingPayload(response, true);
//                            SaveState(response);
//                            var chat = TelegramUtils.GetChatFromStatedMessage(response);
//                            result(true, TelegramUtils.GetChatId(chat));
                        }
                    }
                    else
                    {
                        result(false, null);
                    }
                }
                else
                {
                    result(true, contacts[0].Item2.Id);
                }
            });
        }

        public Task GetContactFromAddress(string address, Action<Contact, Contact.ID> result)
        {
            return Task.Factory.StartNew(async () =>
            {
                Func<IUser, Tuple<TelegramContact, TelegramContact.ID>> buildContact = user =>
                {
                    var id = new TelegramContact.ID
                    {
                        Service = this,
                        Id = address,
                    };
                    var contact = new TelegramContact
                    {
                        FirstName = TelegramUtils.GetUserName(user),
                        Ids = new List<Contact.ID>
                        {
                            id
                        },
                    };
                    return Tuple.Create(contact, id);
                };
                foreach (var user in _dialogs.Users)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (userId == address)
                    {
                        var tuple = buildContact(user);
                        result(tuple.Item1, tuple.Item2);
                        return;
                    }
                }
                var userContacts = await FetchContacts();
                foreach (var userContact in userContacts)
                {
                    var userId = TelegramUtils.GetUserId(userContact);
                    if (userId == address)
                    {
                        var tuple = buildContact(userContact);
                        result(tuple.Item1, tuple.Item2);
                        return;
                    }
                }
                result(null, null);
            });
        }

        public int MaximumParticipants
        {
            get
            {
                return 1000;
            }
        }

        public bool FastSearch
        {
            get
            {
                return true;
            }
        }

        public bool CanAddContact
        {
            get
            {
                return false;
            }
        }
    }
}

