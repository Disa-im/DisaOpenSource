using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SharpTelegram.Schema;
using System.Linq;
using System.Globalization;
using SharpMTProto;
using System.Diagnostics.Contracts;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : INewMessage
    {
        Task INewMessage.GetContacts(string query, bool searchForParties, Action<List<Contact>> result)
        {
            Contract.Ensures(Contract.Result<Task>() != null);
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
                            User = x,
                            Ids = new List<Contact.ID>
                            {
                                    new Contact.ID
                                    {
                                        Service = this,
                                        Id = x.Id.ToString(CultureInfo.InvariantCulture)
                                    }
                            },
					}).Where(x => !string.IsNullOrWhiteSpace(x.FirstName)).OfType<Contact>().OrderBy(x => x.FirstName).ToList();
                    if (string.IsNullOrWhiteSpace(query))
                    {
                        result(contacts);
                    }
                    else
                    {
                        if (query.Length >= 5)
                        {
                            var localContacts = contacts.FindAll(x => Utils.Search(x.FullName, query));
                            using (var client = new FullClientDisposable(this))
                            {
                                var searchResult =
                                    TelegramUtils.RunSynchronously(
                                        client.Client.Methods.ContactsSearchAsync(new ContactsSearchArgs
                                        {
                                            Q = query,
                                            Limit = 50 //like the official client
                                        }));
                                var contactsFound = searchResult as ContactsFound;
                                var globalContacts = GetGlobalContacts(contactsFound);
                                localContacts.AddRange(globalContacts);
                            }
                            result(localContacts);
                        }
                        else
                        {
                            result(contacts.FindAll(x => Utils.Search(x.FullName, query)));
                        }
                    }
                }
                else
                {
                    var partyContacts = new List<Contact>();
                    foreach (var chat in _dialogs.GetAllChats())
                    {
                        var name = TelegramUtils.GetChatTitle(chat);
                        var upgraded = TelegramUtils.GetChatUpgraded(chat);
                        if (upgraded)
                            continue;
                        var left = TelegramUtils.GetChatLeft(chat);
                        if (left)
                            continue;
                        var kicked = TelegramUtils.GetChatKicked(chat);
                        if (kicked)
                            continue;
                        partyContacts.Add(new TelegramPartyContact
                        {
                            FirstName = name,
                            Ids = new List<Contact.ID>
                                {
                                    new Contact.PartyID
                                    {
                                        Service = this,
                                        Id = TelegramUtils.GetChatId(chat),
                                        ExtendedParty = chat is Channel
                                    }
                                },
                        });
                    }

                    //partyContacts.Sort((x, y) => x.FullName.CompareTo(y.FullName));


                    if (string.IsNullOrWhiteSpace(query))
                    {
                        result(partyContacts);
                    }
                    else
                    {
                        if (query.Length >= 5)
                        {
                            var localContacts = partyContacts.FindAll(x => Utils.Search(x.FirstName, query));
                            using (var client = new FullClientDisposable(this))
                            {
                                var searchResult =
                                    TelegramUtils.RunSynchronously(
                                        client.Client.Methods.ContactsSearchAsync(new ContactsSearchArgs
                                        {
                                            Q = query,
                                            Limit = 50 //like the official client
                                        }));
                                var contactsFound = searchResult as ContactsFound;
                                var globalContacts = GetGlobalPartyContacts(contactsFound);
                                localContacts.AddRange(globalContacts);
                            }
                            result(localContacts);
                        }
                        else
                        {
                            var searchResult = partyContacts.FindAll(x => Utils.Search(x.FirstName, query));
                            result(searchResult);
                        }
                    }

                }
            });

        }

        private List<Contact> GetGlobalPartyContacts(ContactsFound contactsFound)
        {
            var globalContacts = new List<Contact>();
            _dialogs.AddChats(contactsFound.Chats);
            Utils.DebugPrint("Chats found " + ObjectDumper.Dump(contactsFound.Chats));
            foreach (var chat in contactsFound.Chats)
            {
                var name = TelegramUtils.GetChatTitle(chat);
                var kicked = TelegramUtils.GetChatKicked(chat);
                if (kicked)
                    continue;
                globalContacts.Add(new TelegramPartyContact
                {
                    FirstName = name,
                    Ids = new List<Contact.ID>
                            {
                                new Contact.PartyID
                                {
                                    Service = this,
                                    Id = TelegramUtils.GetChatId(chat),
                                    ExtendedParty = chat is Channel
                                }
                            },
                });
            }
            return globalContacts;
        }

        private List<Contact> GetGlobalContacts(ContactsFound contactsFound)
        {
            var globalContacts = new List<Contact>();
            _dialogs.AddUsers(contactsFound.Users);
            foreach (var iUser in contactsFound.Users)
            {
                var user = iUser as User;
                if (user != null && user.Bot == null)
                {
                    globalContacts.Add(new TelegramContact
                    {
                        Available = TelegramUtils.GetAvailable(user),
                        LastSeen = TelegramUtils.GetLastSeenTime(user),
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        User = user,
                        Ids = new List<Contact.ID>
                        {
                            new Contact.ID
                            {
                                Service = this,
                                Id = user.Id.ToString(CultureInfo.InvariantCulture)
                            }
                        },
                    });
                }
            }
            return globalContacts;
        }

        public Task GetContactsFavorites(Action<List<Contact>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var count = 0;
                var users = new List<IUser>();
                foreach (var bubbleGroup in BubbleGroupManager.SortByMostPopular(this, true))
                {
                    var address = bubbleGroup.Address;
                    var user = _dialogs.GetUser(uint.Parse(address));
                    if (user != null)
                    {
                        users.Add(user);
                    }
                    count++;
                    if (count > 6)
                        break;
                }

                if (!users.Any())
                {
                    result(null);
                    return;
                }

                var contacts = users.Select(x =>
                    new TelegramContact
                    {
                        Available = TelegramUtils.GetAvailable(x),
                        LastSeen = TelegramUtils.GetLastSeenTime(x),
                        FirstName = TelegramUtils.GetUserName(x),
                        Ids = new List<Contact.ID>
                        {
                            new Contact.ID
                            {
                                Service = this,
                                Id = TelegramUtils.GetUserId(x).ToString(CultureInfo.InvariantCulture)
                            }
                        },
                        User = x as User
                    }).OfType<Contact>().OrderBy(x => x.FirstName).ToList();

                result(contacts);
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
                    var inputUsers = new List<IInputUser>();
                    var names = new List<string>();
                    foreach (var contact in contacts)
                    {
                        names.Add(contact.Item1.FullName);
                        var telegramContact = contact.Item1 as TelegramContact;
                        var inputUser = TelegramUtils.CastUserToInputUser(telegramContact.User);
                        if (inputUser != null)
                        {
                            inputUsers.Add(inputUser);
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
                            try
                            {
                                var response = await client.Client.Methods.MessagesCreateChatAsync(
                                    new MessagesCreateChatArgs
                                    {
                                        Users = inputUsers,
                                        Title = subject,
                                    });
                                var updates = response as Updates;
                                if (updates != null)
                                {
                                    SendToResponseDispatcher(updates, client.Client);
                                    _dialogs.AddUsers(updates.Users);
                                    _dialogs.AddChats(updates.Chats);
                                    var chat = TelegramUtils.GetChatFromUpdate(updates);
                                    result(true, TelegramUtils.GetChatId(chat));
                                }
                                else
                                {
                                    result(false, null);
                                }
                            }
                            catch (Exception e)
                            {
                                //we get an exception if the user is not allowed to create groups
                                var rpcError = e as RpcErrorException;
                                if (rpcError != null)
                                {
                                    result(false, null);
                                }
                            }
                        }
                    }
                    else
                    {
                        result(false, null);
                    }
                }
                else
                {
                    if (contacts[0].Item2 is Contact.PartyID)
                    {
                        JoinChannelIfLeft(contacts[0].Item2.Id);
                        result(true, contacts[0].Item2.Id);
                    }
                    else
                    {
                        var telegramContact = contacts[0].Item1 as TelegramContact;
                        _dialogs.AddUser(telegramContact.User);
                        result(true, contacts[0].Item2.Id);
                    }
                }
            });
        }

        private void JoinChannelIfLeft(string id)
        {
            var channel = _dialogs.GetChat(uint.Parse(id)) as Channel;
            if (channel != null)
            {
                if (channel.Left != null)
                {
                    using (var client = new FullClientDisposable(this))
                    {
                        var updates = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsJoinChannelAsync(new ChannelsJoinChannelArgs
                        {
                            Channel = new InputChannel
                            {
                                ChannelId = uint.Parse(id),
                                AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(id)))
                            }
                        }));
                        SendToResponseDispatcher(updates, client.Client);
                    }
                }
            }
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
                if (address == null)
                {
                    return;
                }

                var userTuple = _dialogs.GetUser(uint.Parse(address));
                if (userTuple != null)
                {
                    var tuple = buildContact(userTuple);
                    result(tuple.Item1, tuple.Item2);
                    return;
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

