using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IPrivacyList
    {
        private string[] previousAddresses;
        public Task GetPrivacyList(Action<string[]> addresses)
        {
            List<string> addressList;
            return Task.Factory.StartNew(() =>
            {
                uint offset = 0;
                //TODO: check when it returns a chunk or a full list
                using (var client = new FullClientDisposable(this))
                {

                Again:
                    var blocked =
                        TelegramUtils.RunSynchronously(
                            client.Client.Methods.ContactsGetBlockedAsync(new ContactsGetBlockedArgs
                            {
                                Limit = 100,
                                Offset = offset
                            }));
                    var contactsBlocked = blocked as ContactsBlocked;
                    var contactsBlockedSlice = blocked as ContactsBlockedSlice;

                    addressList = new List<string>();
                    if (contactsBlocked != null)
                    {
                        foreach (var blockedContact in contactsBlocked.Blocked)
                        {
                            var contactBlocked = blockedContact as ContactBlocked;
                            if (contactBlocked == null) continue;
                            addressList.Add(contactBlocked.UserId.ToString(CultureInfo.InvariantCulture));
                        }
                        _dialogs.AddUsers(contactsBlocked.Users);
                    }
                    if (contactsBlockedSlice != null)
                    {
                        foreach (var blockedContact in contactsBlockedSlice.Blocked)
                        {
                            var contactBlocked = blockedContact as ContactBlocked;
                            if (contactBlocked == null) continue;
                            addressList.Add(contactBlocked.UserId.ToString(CultureInfo.InvariantCulture));
                        }
                        _dialogs.AddUsers(contactsBlockedSlice.Users);
                        if (contactsBlockedSlice.Blocked.Any())
                        {
                            offset += contactsBlockedSlice.Count;
                            goto Again;
                        }
                    }
                    previousAddresses = addressList.ToArray();
                    addresses(addressList.ToArray());
                }
            });
        }

        public Task SetPrivacyList(string[] addresses)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    if (addresses.Length < previousAddresses.Length)
                    {
                        //address was removed
                        foreach (var address in previousAddresses)
                        {
                            if (addresses.Contains(address)) { continue;}

                            TelegramUtils.RunSynchronously(
                                client.Client.Methods.ContactsUnblockAsync(new ContactsUnblockArgs
                                {
                                    Id = new InputUser
                                    {
                                        UserId = uint.Parse(address),
                                        AccessHash = GetUserAccessHashIfForeign(address)
                                    }
                                }));
                            break;

                        }


                    }
                    else //address was added
                    {
                        foreach (var address in addresses)
                        {
                            if (previousAddresses.Contains(address)) { continue; }

                            TelegramUtils.RunSynchronously(
                                client.Client.Methods.ContactsBlockAsync(new ContactsBlockArgs
                                {
                                    Id = new InputUser
                                    {
                                        UserId = uint.Parse(address),
                                        AccessHash = GetUserAccessHashIfForeign(address)
                                    }
                                }));
                            break;

                        }
                    } 
                }
                ServiceEvents.RaisePrivacyListUpdated();
                previousAddresses = addresses;
            });
        }

        public Task GetAddressPicture(string address, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(GetThumbnail(address, false, true));
            });
        }

        public Task GetAddressTitle(string address, Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(TelegramUtils.GetUserName(_dialogs.GetUser(uint.Parse(address))));
            });
        }
    }
}
