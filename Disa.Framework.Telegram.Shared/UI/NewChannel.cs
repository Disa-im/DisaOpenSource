using SharpMTProto;
using SharpTelegram.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : INewChannel
    {
        public Task InviteToChannel(BubbleGroup group, Tuple<Contact, Contact.ID>[] contacts, Action<bool> result)
        {
            var addPartyResult = AddPartyResult.Success;

            return Task.Factory.StartNew(() =>
            {
                if (contacts.Length > 0)
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
                        using (var client = new FullClientDisposable(this))
                        {
                            try
                            {
                                var update = TelegramUtils.RunSynchronously(client.Client.Methods.ChannelsInviteToChannelAsync(new ChannelsInviteToChannelArgs
                                {
                                    Channel = new InputChannel
                                    {
                                        ChannelId = uint.Parse(group.Address),
                                        AccessHash = TelegramUtils.GetChannelAccessHash(_dialogs.GetChat(uint.Parse(group.Address)))
                                    },
                                    Users = inputUsers
                                }));
                                SendToResponseDispatcher(update, client.Client);
                            }
                            catch (Exception ex)
                            {
                                Utils.DebugPrint("Failed Telegram ChannelsInviteToChannelAsync: " + ex.Message);

                                if (ex.Message.Contains("PEER_FLOOD"))
                                {
                                    _addPartyResult = AddPartyResult.Flood;
                                }
                                else
                                {
                                    _addPartyResult = AddPartyResult.Error;
                                }
                                return;
                            }
                        }
                    }
                }
            });
        }

        public Task FetchChannelBubbleGroup(Contact.ID[] contactIds, Action<BubbleGroup> result)
        {
            // If we have a channel group based on a SINGLE Contact.ID 
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
                                // Sanity check, make sure we HAVE a Disa Channel
                                var channel = _dialogs.GetChat(uint.Parse(group.Address)) as Channel;
                                if (channel != null &&
                                    channel.Broadcast != null)
                                {
                                    result(group);
                                }
                                else
                                {
                                    result(null);
                                }

                                return;
                            }
                        }
                    }
                }

                result(null);
            });
        }

        public Task FetchChannelBubbleGroupAddress(string name, string description, Action<bool, string> result)
        {
            return Task.Factory.StartNew(async () =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    try
                    {
                        var response = await client.Client.Methods.ChannelsCreateChannelAsync(new ChannelsCreateChannelArgs
                        {
                            Flags = 0,
                            Broadcast = null,
                            Megagroup = null,
                            Title = name,
                            About = description
                        });

                        var updates = response as Updates;
                        if (updates != null)
                        {
                            SendToResponseDispatcher(updates, client.Client);
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
            });
        }

        public Task GetChannelContacts(string query, Action<List<Contact>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var partyContacts = new List<Contact>();

                // Only grab disa channel groups.
                // Important: Don't get confused between disa channels and telegram channels.
                //            Telegram channels include both supergroups and channels, differentiated 
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
                        if (channel.Broadcast == null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
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
                                        ExtendedParty = true
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
                            var globalContacts = GetGlobalPartyContacts(contactsFound: contactsFound, forChannels: true);
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
            });
        }
    }
}

