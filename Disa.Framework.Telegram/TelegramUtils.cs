using System;
using SharpTelegram.Schema;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;

namespace Disa.Framework.Telegram
{
    public static class TelegramUtils
    {
        public static T RunSynchronously<T>(Task<T> task)
        {
            try
            {
                task.Wait();
                return task.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.Flatten().InnerException;
            }
        }

        public static void RunSynchronously(Task task)
        {
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.Flatten().InnerException;
            }
        }

        public static IInputUser CastUserToInputUser(IUser iUser)
        {
            var userEmpty = iUser as UserEmpty;
            var user = iUser as User;
            if (userEmpty != null)
            {
                return new InputUserEmpty
                {
                    // nothing
                };
            }
            if (user != null)
            {
                return new InputUser
                {
                    UserId = user.Id,
                    AccessHash = user.AccessHash,
                };
            }
            return null;
        }

        public static string GetChatTitle(IChat chat)
        {
            var chatEmpty = chat as ChatEmpty;
            var chatForbidden = chat as ChatForbidden;
            var chatChat = chat as Chat;
            if (chatEmpty != null)
            {
                return chatEmpty.Id.ToString(CultureInfo.InvariantCulture);
            }
            if (chatForbidden != null)
            {
                return chatForbidden.Title;
            }
            if (chatChat != null)
            {
                return chatChat.Title;
            }
            return null;
        }

        public static void SetChatTitle(IChat chat, string title)
        {
            var chatForbidden = chat as ChatForbidden;
            var chatChat = chat as Chat;
            if (chatForbidden != null)
            {
                chatForbidden.Title = title;
            }
            if (chatChat != null)
            {
                chatChat.Title = title;
            }
        }

        public static string GetPeerId(IPeer peer)
        {
            var peerChat = peer as PeerChat;
            var peerUser = peer as PeerUser;
            if (peerChat != null)
            {
                return peerChat.ChatId.ToString(CultureInfo.InvariantCulture);
            }
            if (peerUser != null)
            {
                return peerUser.UserId.ToString(CultureInfo.InvariantCulture);
            }
            return null;
        }

        public static string GetChatId(IChat chat)
        {
            var chatEmpty = chat as ChatEmpty;
            var chatForbidden = chat as ChatForbidden;
            var chatChat = chat as Chat;
            if (chatEmpty != null)
            {
                return chatEmpty.Id.ToString(CultureInfo.InvariantCulture);
            }
            if (chatForbidden != null)
            {
                return chatForbidden.Id.ToString(CultureInfo.InvariantCulture);
            }
            if (chatChat != null)
            {
                return chatChat.Id.ToString(CultureInfo.InvariantCulture);
            }
            return null;
        }

        public static string ConvertTelegramPhoneNumberIntoInternational(string phoneNumber)
        {
            return PhoneBook.TryGetPhoneNumberLegible("+" + phoneNumber);
        }

        public static string GetUserPhoneNumber(IUser iUser)
        {
            var userEmpty = iUser as UserEmpty;
            var user = iUser as User;
            if (userEmpty != null)
            {
                return null;
            }
            if (user != null)
            {
                return user.Phone;
            }
            return null;
        }

        public static string GetUserName(IUser iUser)
        {
            var userEmpty = iUser as UserEmpty;
            var user = iUser as User;
            if (userEmpty != null)
            {
                return userEmpty.Id.ToString(CultureInfo.InvariantCulture);
            }
            if (user != null)
            {
                return user.FirstName + " " + user.LastName;
            }
            return null;
        }

        public static ulong GetAccessHash(IUser iUser)
        {
            var user = iUser as User;
            if (user != null)
            {
                return user.AccessHash;
            }
            return 0;
        }

        public static FileLocation GetChatThumbnailLocation(IChat chat, bool small)
        {
            var chatEmpty = chat as ChatEmpty;
            var chatForbidden = chat as ChatForbidden;
            var chatChat = chat as Chat;
            if (chatEmpty != null)
            {
                return null;
            }
            if (chatForbidden != null)
            {
                return null;
            }
            if (chatChat != null)
            {
                return GetFileLocationFromPhoto(chatChat.Photo, small);
            }
            return null;
        }

        public static FileLocation GetUserPhotoLocation(IUser iUser, bool small)
        {
            var userEmpty = iUser as UserEmpty;
            var user = iUser as User;
            if (userEmpty != null)
            {
                return null;
            }
            if (user != null)
            {
                return GetFileLocationFromPhoto(user.Photo, small);
            }
            return null;
        }

        private static FileLocation GetFileLocationFromPhoto(IChatPhoto photo, bool small)
        {
            var empty = photo as ChatPhotoEmpty;
            var full = photo as ChatPhoto;
            if (empty != null)
            {
                return null;
            }
            else if (full != null)
            {
                var iFileLocation = small ? full.PhotoSmall : full.PhotoBig;
                var fileLocation = iFileLocation as FileLocation;
                if (fileLocation != null)
                {
                    return fileLocation;
                }
                else
                {
                    // If the file location is empty, then we assume the chat hasn't set a photo.
                    // Fall-through
                }
            }
            return null;
        }

        private static FileLocation GetFileLocationFromPhoto(IUserProfilePhoto photo, bool small)
        {
            var empty = photo as UserProfilePhotoEmpty;
            var full = photo as UserProfilePhoto;
            if (empty != null)
            {
                return null;
            }
            else if (full != null)
            {
                var iFileLocation = small ? full.PhotoSmall : full.PhotoBig;
                var fileLocation = iFileLocation as FileLocation;
                if (fileLocation != null)
                {
                    return fileLocation;
                }
                else
                {
                    // If the file location is empty, then we assume the user hasn't set a photo.
                    // Fall-through
                }
            }
            return null;
        }

        public static string GetUserId(IUser iUser)
        {
            var userEmpty = iUser as UserEmpty;
            var user = iUser as User;
            if (userEmpty != null)
            {
                return userEmpty.Id.ToString(CultureInfo.InvariantCulture);
            }
            if (user != null)
            {
                return user.Id.ToString(CultureInfo.InvariantCulture);
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

        public static IUserStatus GetStatus(IUser iUser)
        {
            var userEmpty = iUser as UserEmpty;
            var user = iUser as User;
            if (userEmpty != null)
            {
                return null;
            }
            if (user != null)
            {
                return user.Status;
            }
            return null;
        }
    }
}

