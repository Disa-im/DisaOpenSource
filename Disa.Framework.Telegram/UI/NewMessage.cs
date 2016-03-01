using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SharpTelegram.Schema.Layer18;
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
                //TODO: search for parties
                using (var client = new FullClientDisposable(this))
                {
                    var response = (ContactsContacts)await client.Client.Methods.ContactsGetContactsAsync(
                        new ContactsGetContactsArgs
                        {
                            Hash = string.Empty
                        });
                    var users = response.Users.OfType<UserContact>();
                    var contacts = users.Select(x => 
                        new TelegramContact 
                        {
                            Available = TelegramUtils.GetAvailable(x),
                            LastSeen =  TelegramUtils.GetLastSeenTime(x),
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
            });
        }

        public Task GetContactsFavorites(Action<List<Contact>> result)
        {
            return Task.Factory.StartNew(() =>
                {
                    result(null);
                });
        }

        public Task GetContactPhoto(Contact contact, bool preview, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
                {
                    result(GetThumbnail(contact.Ids[0].Id, false, preview));
                });
        }

        public Task FetchBubbleGroup(Contact.ID[] contactIds, Action<BubbleGroup> result)
        {
            return Task.Factory.StartNew(() =>
                {
                    foreach (var group in BubbleGroupManager.FindAll(this))
                    {
                        if (group.IsParty)
                            continue;

                        foreach (var contactId in contactIds)
                        {
                            if (BubbleGroupComparer(contactId.Id, group.Address))
                            {
                                result(group);
                                return;
                            }
                        }
                    }
                        
                    result(null);
                });
        }

        public Task FetchBubbleGroupAddress(Tuple<Contact, Contact.ID>[] contacts, Action<bool, string> result)
        {
            return Task.Factory.StartNew(() =>
                {
                    if (contacts.Length > 1)
                    {
                        //TODO: party support
                    }
                    else
                    {
                        result(true, contacts[0].Item2.Id);
                    }
                });
        }

        public Task GetContactFromAddress(string address, Action<Contact, Contact.ID> result)
        {
            return Task.Factory.StartNew(() =>
                {
                    DebugPrint("ssssomething found");
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

