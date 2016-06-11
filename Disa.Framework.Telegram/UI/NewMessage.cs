using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SharpTelegram.Schema;
using System.Linq;
using System.Globalization;
using SharpMTProto;

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
                            User = x,
                            Ids = new List<Contact.ID>
                            {
                                    new Contact.ID
                                    {
                                        Service = this,
                                        Id = x.Id.ToString(CultureInfo.InvariantCulture)
                                    }
                            },
                        }).OfType<Contact>().OrderBy(x => x.FirstName).ToList();
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
                                DebugPrint("####### contacts found " + ObjectDumper.Dump(contactsFound.Users));
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
                        partyContacts.Add(new TelegramPartyContact
                        {
                            FirstName = name,
                            Ids = new List<Contact.ID>
                                {
                                    new Contact.PartyID
                                    {
                                        Service = this,
                                        Id = TelegramUtils.GetChatId(chat),
                                    }
                                },
                        });
                    }

                    if (string.IsNullOrWhiteSpace(query))
                    {
                        result(partyContacts);
                    }
                    else
                    {
                        result(partyContacts.FindAll(x => Utils.Search(x.FullName, query)));
                    }

                }


            });

        }

        private List<Contact> GetGlobalContacts(ContactsFound contactsFound)
        {
            var globalContacts = new List<Contact>();
            foreach (var iUser in contactsFound.Users)
            {
                var user = iUser as User;
                if (user != null)
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
        //TODO: update to just fetch from the cache
        public Task GetContactsFavorites(Action<List<Contact>> result)
        {
            return Task.Factory.StartNew(async () =>
            {
                var count = 0;
                var inputUsers = new List<IInputUser>();
                foreach (var bubbleGroup in BubbleGroupManager.SortByMostPopular(this, true))
                {
                    var address = bubbleGroup.Address;
                    var user = _dialogs.GetUser(uint.Parse(address));
                    if (user != null)
                    {
                        var inputUser = TelegramUtils.CastUserToInputUser(user);
                        if (inputUser != null)
                        {
                            inputUsers.Add(inputUser);
                            break;
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
                    var telegramContact = contacts[0].Item1 as TelegramContact;
                    _dialogs.AddUser(telegramContact.User);
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

