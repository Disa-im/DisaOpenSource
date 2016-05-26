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

		public static IChat GetChatFromStatedMessage(IMessage message)
        {
//            var messagesStatedMessage = message as MessagesStatedMessage;
//            if (messagesStatedMessage != null)
//            {
//                return messagesStatedMessage.Chats.FirstOrDefault();
//            }
//            var messagesStatedMessageLink = message as MessagesStatedMessageLink;
//            if (messagesStatedMessageLink != null)
//            {
//                return messagesStatedMessageLink.Chats.FirstOrDefault();
//            }
            return null;
        }

        public static IInputUser CastUserToInputUser(IUser user)
        {
//            var userEmpty = user as UserEmpty;
////            var userSelf = user as UserSelf;
////            var userContact = user as UserContact;
////            var userForeign = user as UserForeign;
//            if (userEmpty != null)
//            {
//                return new InputUserEmpty
//                {
//                    // nothing
//                };
//            }
//            if (userSelf != null)
//            {
//                return new InputUserSelf
//                {
//                    // nothing
//                };
//            }
//            if (userContact != null)
//            {
//                return new InputUserContact
//                {
//                    UserId = userContact.Id
//                };
//            }
//            if (userForeign != null)
//            {
//                return new InputUserForeign
//                {
//                    UserId = userForeign.Id,
//                    AccessHash = userForeign.AccessHash,
//                };
//            }
            return null;
        }

        public static string GetChatTitle(IChat chat)
        {
            var chatEmpty = chat as ChatEmpty;
            var chatForbidden = chat as ChatForbidden;
            var chatChat = chat as Chat;
//            var geoChat = chat as GeoChat;
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
//            if (geoChat != null)
//            {
//                return geoChat.Title;
//            }
            return null;
        }

        public static void SetChatTitle(IChat chat, string title)
        {
            var chatForbidden = chat as ChatForbidden;
            var chatChat = chat as Chat;
//            var geoChat = chat as GeoChat;
            if (chatForbidden != null)
            {
                chatForbidden.Title = title;
            }
            if (chatChat != null)
            {
                chatChat.Title = title;
            }
//            if (geoChat != null)
//            {
//                geoChat.Title = title;
//            }
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
//            var geoChat = chat as GeoChat;
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
//            if (geoChat != null)
//            {
//                return geoChat.Id.ToString(CultureInfo.InvariantCulture);
//            }
            return null;
        }

        public static string ConvertTelegramPhoneNumberIntoInternational(string phoneNumber)
        {
            return PhoneBook.TryGetPhoneNumberLegible("+" + phoneNumber);
        }

        public static string GetUserPhoneNumber(IUser user)
        {
            var userEmpty = user as UserEmpty;
//            var userSelf = user as UserSelf;
//            var userContact = user as UserContact;
//            var userRequest = user as UserRequest;
//            var userDeleted = user as UserDeleted;
//            var userForeign = user as UserForeign;
//            if (userEmpty != null)
//            {
//                return null;
//            }
//            if (userSelf != null)
//            {
//                return userSelf.Phone;
//            }
//            if (userContact != null)
//            {
//                return userContact.Phone;
//            }
//            if (userRequest != null)
//            {
//                return userRequest.Phone;
//            }
//            if (userDeleted != null)
//            {
//                return null;
//            }
//            if (userForeign != null)
//            {
//                return null;
//            }
            return null;
        }

        public static string GetUserName(IUser user)
        {
            var userEmpty = user as UserEmpty;
//            var userSelf = user as UserSelf;
//            var userContact = user as UserContact;
//            var userRequest = user as UserRequest;
//            var userDeleted = user as UserDeleted;
//            var userForeign = user as UserForeign;
//            if (userEmpty != null)
//            {
//                return userEmpty.Id.ToString(CultureInfo.InvariantCulture);
//            }
//            if (userSelf != null)
//            {
//                return userSelf.FirstName + " " + userSelf.LastName;
//            }
//            if (userContact != null)
//            {
//                return userContact.FirstName + " " + userContact.LastName;
//            }
//            if (userRequest != null)
//            {
//                return userRequest.FirstName + " " + userRequest.LastName;
//            }
//            if (userDeleted != null)
//            {
//                return userDeleted.FirstName + " " + userDeleted.LastName;
//            }
//            if (userForeign != null)
//            {
//                return userForeign.FirstName + " " + userForeign.LastName;
//            }
            return null;
        }

        public static ulong GetAccessHash(IUser user)
        {
//            var userContact = user as UserContact;
//            var userRequest = user as UserRequest;
//            var userForeign = user as UserForeign;
//            if (userContact != null)
//            {
//                return userContact.AccessHash;
//            }
//            if (userRequest != null)
//            {
//                return userRequest.AccessHash;
//            }
//            if (userForeign != null)
//            {
//                return userForeign.AccessHash;
//            }
            return 0;
        }

        public static FileLocation GetChatThumbnailLocation(IChat chat, bool small)
        {
            var chatEmpty = chat as ChatEmpty;
            var chatForbidden = chat as ChatForbidden;
            var chatChat = chat as Chat;
//            var geoChat = chat as GeoChat;
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
//            if (geoChat != null)
//            {
//                return GetFileLocationFromPhoto(geoChat.Photo, small);
//            }
            return null;
        }

        public static FileLocation GetUserPhotoLocation(IUser user, bool small)
        {
            var userEmpty = user as UserEmpty;
//            var userSelf = user as UserSelf;
//            var userContact = user as UserContact;
//            var userRequest = user as UserRequest;
//            var userDeleted = user as UserDeleted;
//            var userForeign = user as UserForeign;
//            if (userEmpty != null)
//            {
//                return null;
//            }
//            if (userSelf != null)
//            {
//                return GetFileLocationFromPhoto(userSelf.Photo, small);
//            }
//            if (userContact != null)
//            {
//                return GetFileLocationFromPhoto(userContact.Photo, small);
//            }
//            if (userRequest != null)
//            {
//                return GetFileLocationFromPhoto(userRequest.Photo, small);
//            }
//            if (userDeleted != null)
//            {
//                return null;
//            }
//            if (userForeign != null)
//            {
//                return GetFileLocationFromPhoto(userForeign.Photo, small);
//            }
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

        public static string GetUserId(IUser user)
        {
            var userEmpty = user as UserEmpty;
//            var userSelf = user as UserSelf;
//            var userContact = user as UserContact;
//            var userRequest = user as UserRequest;
//            var userDeleted = user as UserDeleted;
//            var userForeign = user as UserForeign;
//            if (userEmpty != null)
//            {
//                return userEmpty.Id.ToString(CultureInfo.InvariantCulture);
//            }
//            if (userSelf != null)
//            {
//                return userSelf.Id.ToString(CultureInfo.InvariantCulture);
//            }
//            if (userContact != null)
//            {
//                return userContact.Id.ToString(CultureInfo.InvariantCulture);
//            }
//            if (userRequest != null)
//            {
//                return userRequest.Id.ToString(CultureInfo.InvariantCulture);
//            }
//            if (userDeleted != null)
//            {
//                return userDeleted.Id.ToString(CultureInfo.InvariantCulture);
//            }
//            if (userForeign != null)
//            {
//                return userForeign.Id.ToString(CultureInfo.InvariantCulture);
//            }
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
//            var userSelf = user as UserSelf;
//            var userContact = user as UserContact;
//            var userRequest = user as UserRequest;
//            var userDeleted = user as UserDeleted;
//            var userForeign = user as UserForeign;
//            if (userEmpty != null)
//            {
//                return null;
//            }
//            if (userSelf != null)
//            {
//                return userSelf.Status;
//            }
//            if (userContact != null)
//            {
//                return userContact.Status;
//            }
//            if (userRequest != null)
//            {
//                return userRequest.Status;
//            }
//            if (userDeleted != null)
//            {
//                return null;
//            }
//            if (userForeign != null)
//            {
//                return userForeign.Status;
//            }
            return null;
        }
    }
}

