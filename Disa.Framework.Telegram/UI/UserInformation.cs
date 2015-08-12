using System;
using System.Threading.Tasks;
using SharpTelegram.Schema.Layer18;
using System.Linq;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IUserInformation
    {
        public Task GetUserInformationThumbnail(string address, bool preview, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(GetThumbnail(address, false, preview));
            });
        }

        private bool PhoneBookHasNumber(string number)
        {
            var contacts = PhoneBook.PhoneBookContacts;
            if (!contacts.Any())
            {
                return false;
            }
            foreach (var contact in contacts)
            {
                foreach (var testNumber in contact.PhoneNumbers)
                {
                    if (PhoneBook.PhoneNumberComparer(number, testNumber.Number))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Task GetUserInformationSecondaryActionThumbnail(string address, Action<byte[]> result)
        {
            return Task.Factory.StartNew(() =>
            {
                foreach (var user in _dialogs.Users)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (userId == address)
                    {
                        var phoneNumber = TelegramUtils.GetUserPhoneNumber(user);
                        if (!string.IsNullOrWhiteSpace(phoneNumber))
                        {
                            if (PhoneBookHasNumber(phoneNumber))
                            {
                                result(Platform.GetIcon(IconType.People));
                            }
                            else
                            {
                                result(Platform.GetIcon(IconType.AddContact));
                            }
                        }
                        else
                        {
                            result(null);
                        }
                        return;
                    }
                }
                result(null);
            });
        }

        public Task ClickUserInformationSecondaryAction(string address)
        {
            return Task.Factory.StartNew(() =>
            {
                foreach (var user in _dialogs.Users)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (userId == address)
                    {
                        var phoneNumber = TelegramUtils.GetUserPhoneNumber(user);
                        if (!string.IsNullOrWhiteSpace(phoneNumber))
                        {
                            Platform.OpenContact(
                                TelegramUtils.ConvertTelegramPhoneNumberIntoInternational(phoneNumber));
                            return;
                        }
                    }
                }
            });
        }

        public Task GetUserInformationPrimaryActionThumbnail(string address, Action<byte[]> result)
        {
            return Task.Factory.StartNew(() =>
            {
                foreach (var user in _dialogs.Users)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (userId == address)
                    {
                        var phoneNumber = TelegramUtils.GetUserPhoneNumber(user);
                        if (!string.IsNullOrWhiteSpace(phoneNumber))
                        {
                            result(Platform.GetIcon(IconType.Call));
                            return;
                        }
                    }
                }
                result(null);
            });
        }

        public Task ClickUserInformationPrimaryAction(string address)
        {
            return Task.Factory.StartNew(() =>
            {
                foreach (var user in _dialogs.Users)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (userId == address)
                    {
                        var phoneNumber = TelegramUtils.GetUserPhoneNumber(user);
                        if (!string.IsNullOrWhiteSpace(phoneNumber))
                        {
                            Platform.DialContact(
                                TelegramUtils.ConvertTelegramPhoneNumberIntoInternational(phoneNumber));
                            return;
                        }
                    }
                }
            });
        }

        public Task GetUserInformation(string address, Action<Disa.Framework.UserInformation> result)
        {
            return Task.Factory.StartNew(async () =>
            {
                foreach (var user in _dialogs.Users)
                {
                    var userId = TelegramUtils.GetUserId(user);
                    if (userId == address)
                    {
                        var inputUser = TelegramUtils.CastUserToInputUser(user);
                        var updatedUser = await GetUser(inputUser, _fullClient);
                        var name = TelegramUtils.GetUserName(updatedUser);
                        var lastSeen = TelegramUtils.GetLastSeenTime(updatedUser);
                        var presence = TelegramUtils.GetAvailable(updatedUser);
                        var phoneNumber = TelegramUtils.GetUserPhoneNumber(updatedUser);

                        if (string.IsNullOrWhiteSpace(name))
                        {
                            result(null);  //TODO: ensure this doesn't crash Disa
                            return;
                        }
                        result(new UserInformation
                        {
                            Title = name,
                            SubtitleType = string.IsNullOrWhiteSpace(phoneNumber) ? 
                                UserInformation.TypeSubtitle.Other : UserInformation.TypeSubtitle.PhoneNumber,
                            Subtitle = string.IsNullOrWhiteSpace(phoneNumber) ? 
                                address : TelegramUtils.ConvertTelegramPhoneNumberIntoInternational(phoneNumber),
                            LastSeen = lastSeen,
                            Presence = presence,
                        });
                        return;
                    }
                }
                result(null);  //TODO: ensure this doesn't crash Disa
            });
        }
    }
}

