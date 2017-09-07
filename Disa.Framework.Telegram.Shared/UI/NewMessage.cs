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
                    var contacts = users.Select(x => CreateTelegramContact(x))
                                   .Where(x => !string.IsNullOrWhiteSpace(x.FirstName)).OfType<Contact>().OrderBy(x => x.FirstName).ToList();
					
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

                    // Only grab disa solo, party and super groups.
                    // Important: Don't get confused between disa channels and telegram channels.
                    //            Telegram channels include both super groups and channels, differentiated 
                    //            by the telegram Channel.Broadcast and Channel.Megagroup fields.

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
                        var isChannel = chat is Channel;
                        if (isChannel)
                        {
                            var channel = chat as Channel;
                            if (channel.Megagroup == null)
                            {
                                continue;
                            }
                        }
                        partyContacts.Add(new TelegramPartyContact
                        {
                            FirstName = name,
                            Ids = new List<Contact.ID>
                                {
                                    new Contact.PartyID
                                    {
                                        Service = this,
                                        Id = TelegramUtils.GetChatId(chat),
                                        ExtendedParty = isChannel
                                    }
                                },
                        });
                    }

                    if (string.IsNullOrWhiteSpace(query))
                    {
                        result(partyContacts.OrderBy(c => c.FirstName).ToList());
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
                                var globalContacts = GetGlobalPartyContacts(contactsFound: contactsFound, forChannels: false);
                                localContacts.AddRange(globalContacts);
                            }
                            result(localContacts.OrderBy(c => c.FirstName).ToList());
                        }
                        else
                        {
                            var searchResult = partyContacts.FindAll(x => Utils.Search(x.FirstName, query));
                            result(searchResult.OrderBy(c => c.FirstName).ToList());
                        }
                    }

                }
            });

        }

        private List<Contact> GetGlobalPartyContacts(ContactsFound contactsFound, bool forChannels)
        {
            var globalContacts = new List<Contact>();
            _dialogs.AddChats(contactsFound.Chats);
            foreach (var chat in contactsFound.Chats)
            {
                var name = TelegramUtils.GetChatTitle(chat);
                var kicked = TelegramUtils.GetChatKicked(chat);
                if (kicked)
                    continue;
                var isChannel = chat is Channel;
                if (forChannels && !isChannel)
                {
                    continue;
                }
                else if (!forChannels && isChannel)
                {
                    continue;
                }
                globalContacts.Add(new TelegramPartyContact
                {
                    FirstName = name,
                    Ids = new List<Contact.ID>
                            {
                                new Contact.PartyID
                                {
                                    Service = this,
                                    Id = TelegramUtils.GetChatId(chat),
                                    ExtendedParty = isChannel
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
                if (user != null)
                {
                    var globalContact = CreateTelegramContact(user);
                    globalContacts.Add(globalContact);
                }
            }
            return globalContacts;
        }
        
        private Contact CreateTelegramContact(User user)
        {
            Contact globalContact;

            if (user.Bot != null)
            {
                globalContact = new TelegramBotContact
                {
                    User = user,
                    BotInlinePlaceholder = user.BotInlinePlaceholder
                };
            }
            else
            {
                globalContact = new TelegramContact
                {
                    User = user
                };
            }

            globalContact.Available = TelegramUtils.GetAvailable(user);
            globalContact.LastSeen = TelegramUtils.GetLastSeenTime(user);
            globalContact.FirstName = user.FirstName;
            globalContact.LastName = user.LastName;
            globalContact.Ids = new List<Contact.ID>
                {
                    new Contact.ID
                    {
                        Service = this,
                        Id = user.Id.ToString(CultureInfo.InvariantCulture)
                    }
                };

            return globalContact;
        }

        public Task GetContactsFavorites(Action<List<Contact>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var count = 0;
                var users = new List<User>();
                foreach (var bubbleGroup in BubbleGroupManager.SortByMostPopular(this, true))
                {
                    var address = bubbleGroup.Address;
                    var user = _dialogs.GetUser(uint.Parse(address)) as User;
                    if (user != null)
                    {
                        users.Add(user);
                    }
                    count++;
                    if (count > 8)
                        break;
                }

                if (!users.Any())
                {
                    result(null);
                    return;
                }

                var contacts = users.Select(x => CreateTelegramContact(x))
                    .OfType<Contact>().OrderBy(x => x.FirstName).ToList();

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
            // If we have a solo, party or super group based on a SINGLE Contact.ID 
            // in our passed in collection return that, otherwise return
            // null
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
                                // Sanity check, make sure we DO NOT HAVE a Disa Channel
                                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                                if (channel != null &&
                                    channel.Broadcast != null)
                                {
                                    result(null);
                                }
                                else
                                {
                                    result(group);
                                }

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
                        IUser user = null;
                        if (contact.Item1 is TelegramContact)
                        {
                            user = (contact.Item1 as TelegramContact).User;
                        }
                        else if (contact.Item1 is TelegramBotContact)
                        {
                            user = (contact.Item1 as TelegramBotContact).User;
                        }
                        var inputUser = TelegramUtils.CastUserToInputUser(user);
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
                    else if (contacts[0].Item1 is TelegramBotContact)
                    {
                        var telegramContact = contacts[0].Item1 as TelegramBotContact;
                        _dialogs.AddUser(telegramContact.User);
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

        private enum LinkType
        {
            PrivateGroup,
            PublicGroup,
            PublicUser,
            Invalid
        }

        public bool SupportsShareLinks
        {
            get
            {
                return true;
            }
        }

        public string SearchHint
        {
            get
            {
                return Localize.GetString("TelegramSearchHint");
            }
        }

        public Task FetchBubbleGroupAddressFromLink(string link, Action<Tuple<Contact, Contact.ID>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                string linkUsefulPart;
                var linkType = GetLinkType(link, out linkUsefulPart);
                switch (linkType)
                {
                    case LinkType.Invalid:
                        result(new Tuple<Contact, Contact.ID>(null, null));
                        break;
                    case LinkType.PrivateGroup:
                        IChat alreadyJoinedChat;
                        bool linkCheck = CheckLink(linkUsefulPart, out alreadyJoinedChat);
                        if (linkCheck)
                        {
                            if (alreadyJoinedChat != null)
                            {
                                SetResultAsChat(alreadyJoinedChat, TelegramUtils.GetChatId(alreadyJoinedChat), result);
                                return;
                            }
                            var updates = JoinChat(linkUsefulPart);
                            var updatesObj = updates as Updates;
                            if (updatesObj != null)
                            {
                                var chatObj = updatesObj.Chats[0];
                                SetResultAsChat(chatObj, TelegramUtils.GetChatId(chatObj), result);
                            }

                        }
                        else
                        {
                            result(new Tuple<Contact, Contact.ID>(null, null));
                        }
                        break;
                    case LinkType.PublicGroup:
                        JoinChannelIfLeft(linkUsefulPart);
                        var chat = _dialogs.GetChat(uint.Parse(linkUsefulPart));
                        SetResultAsChat(chat, linkUsefulPart, result);
                        break;
                    case LinkType.PublicUser:
                        var user = _dialogs.GetUser(uint.Parse(linkUsefulPart));
                        var userObj = user as User;
                        var userContact = new TelegramContact
                        {
                            Available = TelegramUtils.GetAvailable(user),
                            LastSeen = TelegramUtils.GetLastSeenTime(user),
                            FirstName = userObj?.FirstName,
                            LastName = userObj?.LastName,
                            User = userObj,
                        };
                        var userContactId = new Contact.ID
                        {
                            Id = linkUsefulPart,
                            Service = this
                        };
                        result(new Tuple<Contact, Contact.ID>(userContact, userContactId));
                        break;
                    default:
                        result(new Tuple<Contact, Contact.ID>(null, null));
                        break;
                }
            });
        }

        public Task GetContactsByUsername(string query, Action<List<Contact>> result)
        {
            Contract.Ensures(Contract.Result<Task>() != null);
            return Task.Factory.StartNew(() =>
            {
                var resolvedPeer = ResolvePeer(query);
                var contacts = new List<Contact>();
                foreach (var iUser in resolvedPeer.Users)
                {
                    var user = iUser as User;
                    if (user != null)
                    {
                        var contact = CreateTelegramContact(user);
                        contacts.Add(contact);
                    }
                }
                result(contacts);
            });
        }

        private void SetResultAsChat(IChat chat, string id, Action<Tuple<Contact, Contact.ID>> result)
        {
            var contact = new TelegramContact
            {
                FirstName = TelegramUtils.GetChatTitle(chat)
            };
            var contactId = new Contact.PartyID
            {
                ExtendedParty = chat is Channel,
                Id = id,
                Service = this
            };
            result(new Tuple<Contact, Contact.ID>(contact, contactId));
        }

        private IUpdates JoinChat(string linkUsefulPart)
        {
            using (var client = new FullClientDisposable(this))
            {
                try
                {
                    var result = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesImportChatInviteAsync(new MessagesImportChatInviteArgs
                    {
                        Hash = linkUsefulPart
                    }));
                    SendToResponseDispatcher(result, client.Client);
                    return result;
                }
                catch (Exception e)
                {
                    Utils.DebugPrint("Exception e " + e);
                    return null;
                }
            }
        }

        private bool CheckLink(string linkUsefulPart, out IChat alreadyJoinedChat)
        {
            using (var client = new FullClientDisposable(this))
            {
                try
                {
                    var result = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesCheckChatInviteAsync(new MessagesCheckChatInviteArgs
                    {
                        Hash = linkUsefulPart
                    }));
                    var chatInvite = result as ChatInvite;
                    var chatInviteAlready = result as ChatInviteAlready;
                    if (chatInvite != null)
                    {
                        alreadyJoinedChat = null;
                        return true;
                    }
                    if (chatInviteAlready != null)
                    {
                        alreadyJoinedChat = chatInviteAlready.Chat;
                        return true;
                    }
                    alreadyJoinedChat = null;
                    return false;
                }
                catch (Exception ex)
                {
                    DebugPrint("Exception while checking invite" + ex);
                    alreadyJoinedChat = null;
                    return false;
                }
            }
        }

        private LinkType GetLinkType(string link, out string linkUsefulPart)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(link);
            }
            catch (Exception e)
            {
                DebugPrint("Invalid uri" + e);
                linkUsefulPart = null;
                return LinkType.Invalid;
            }
            if (uri != null)
            {
                if (uri.Host != "telegram.me" &&
                    uri.Host != "telegram.dog" &&  //yo dog i dunno why this shit aint a domain til now
                    uri.Host != "t.me")
                {
                    linkUsefulPart = null;
                    return LinkType.Invalid;
                }
                var path = uri.LocalPath;
                if (path.StartsWith("/joinchat"))
                {
                    path = path.Replace("/joinchat/", "");
                    linkUsefulPart = path;
                    return LinkType.PrivateGroup;
                }
                else
                {
                    path = path.Replace("/", "");
                    var resolvedPeer = ResolvePeer(path);
                    if (resolvedPeer?.Peer != null)
                    {
                        //this can only be a channel or a user, cannot be a chat
                        _dialogs.AddUsers(resolvedPeer.Users);
                        _dialogs.AddChats(resolvedPeer.Chats);
                        var peerUser = resolvedPeer.Peer as PeerUser;
                        if (peerUser != null)
                        {
                            linkUsefulPart = peerUser.UserId.ToString();
                            return LinkType.PublicUser;
                        }
                        var peerChannel = resolvedPeer.Peer as PeerChannel;
                        if (peerChannel != null)
                        {
                            linkUsefulPart = peerChannel.ChannelId.ToString();
                            return LinkType.PublicGroup;
                        }
                    }
                }
            }
            linkUsefulPart = null;
            return LinkType.Invalid;
        }

        private ContactsResolvedPeer ResolvePeer(string path)
        {
            using (var client = new FullClientDisposable(this))
            {
                try
                {
                    var result = (ContactsResolvedPeer)TelegramUtils.RunSynchronously(client.Client.Methods.ContactsResolveUsernameAsync(new ContactsResolveUsernameArgs
                    {
                        Username = path
                    }));
                    return result;
                }
                catch (Exception e)
                {
                    DebugPrint("Exception while resolving peer" + e);
                    return null;
                }
            }
        }
    }
}

