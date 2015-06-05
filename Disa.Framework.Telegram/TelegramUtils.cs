using System;
using SharpTelegram.Schema.Layer18;
using System.Globalization;

namespace Disa.Framework.Telegram
{
    public static class TelegramUtils
    {
        public static string GetNameForSoloConversation(IUser user)
        {
            var userEmpty = user as UserEmpty;
            var userSelf = user as UserSelf;
            var userContact = user as UserContact;
            var userRequest = user as UserRequest;
            var userDeleted = user as UserDeleted;
            if (userEmpty != null)
            {
                return userEmpty.Id.ToString(CultureInfo.InvariantCulture);
            }
            if (userSelf != null)
            {
                return userSelf.FirstName + " " + userSelf.LastName;
            }
            if (userContact != null)
            {
                return userContact.FirstName + " " + userContact.LastName;
            }
            if (userRequest != null)
            {
                return userRequest.FirstName + " " + userRequest.LastName;
            }
            if (userDeleted != null)
            {
                return userDeleted.FirstName + " " + userDeleted.LastName;
            }
            return null;
        }

        public static string GetUserId(IUser user)
        {
            var userEmpty = user as UserEmpty;
            var userSelf = user as UserSelf;
            var userContact = user as UserContact;
            var userRequest = user as UserRequest;
            var userDeleted = user as UserDeleted;
            if (userEmpty != null)
            {
                return userEmpty.Id.ToString(CultureInfo.InvariantCulture);
            }
            if (userSelf != null)
            {
                return userSelf.Id.ToString(CultureInfo.InvariantCulture);
            }
            if (userContact != null)
            {
                return userContact.Id.ToString(CultureInfo.InvariantCulture);
            }
            if (userRequest != null)
            {
                return userRequest.Id.ToString(CultureInfo.InvariantCulture);
            }
            if (userDeleted != null)
            {
                return userDeleted.Id.ToString(CultureInfo.InvariantCulture);
            }
            return null;
        }

        public static long GetLastSeenTime(IUser user)
        {
            var status = GetStatus(user);
            return status is UserStatusOffline ? (status as UserStatusOffline).WasOnline : 0;
        }

        public static bool GetAvailable(IUser user)
        {
            var status = GetStatus(user);
            return GetAvailable(status);
        }

        public static bool GetAvailable(IUserStatus status)
        {
            return status is UserStatusOnline;
        }

        public static IUserStatus GetStatus(IUser user)
        {
            var userEmpty = user as UserEmpty;
            var userSelf = user as UserSelf;
            var userContact = user as UserContact;
            var userRequest = user as UserRequest;
            var userDeleted = user as UserDeleted;
            if (userEmpty != null)
            {
                return null;
            }
            if (userSelf != null)
            {
                return userSelf.Status;
            }
            if (userContact != null)
            {
                return userContact.Status;
            }
            if (userRequest != null)
            {
                return userRequest.Status;
            }
            if (userDeleted != null)
            {
                return null;
            }
            return null;
        }
    }
}

