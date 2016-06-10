using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using SharpTelegram.Schema;
using ProtoBuf;

namespace Disa.Framework.Telegram
{
    public class CachedDialogs
    {
        /// <summary>
        /// Cached path for the user db
        /// </summary>
        private string _userPathCached;

        /// <summary>
        /// cached path for the chats db
        /// </summary>
        private string _chatPathCached;


        /// <summary>
        /// Lock for concurrent accesses to the user sql database
        /// </summary>
        private readonly object _userLock;

        /// <summary>
        /// Lock for concurrent access to all chats in the sql database
        /// </summary>
        private readonly object _chatLock;

        /// <summary>
        /// boolean indicating chats have been loaded before
        /// </summary>
        private bool _chatsLoaded;


        /// <summary>
        /// Default constructor
        /// </summary>
        public CachedDialogs()
        {
            _userLock = new object();

            _chatLock  = new object();
        }

        /// <summary>
        /// Loads the serialized user object from the sqlite database, reconstructs it and returns.
        /// </summary>
        /// <param name="userId">the id of the user</param>
        /// <returns></returns>
        public IUser GetUser(uint userId)
        {
            lock (_userLock)
            {
                using (var database = new SqlDatabase<CachedUser>(GetDatabasePath(true)))
                {
                    var user = database.Store.Where(x => x.Id == userId).FirstOrDefault();
                    if (user == null)
                    {
                        return null;
                    }
                    var stream = new MemoryStream(user.ProtoBufBytes);
                    stream.Position = 0;
                    IUser iuser = Serializer.Deserialize<User>(stream);
                    return iuser;
                }
            }
        }

        /// <summary>
        /// Adds a list of users to the current users database in a thread safe way.
        /// </summary>
        /// <param name="users">List of user objects to be added</param>
        public void AddUsers(List<IUser> users)
        {
            lock (_userLock)
            {
                using (var database = new SqlDatabase<CachedUser>(GetDatabasePath(true)))
                {
                    foreach (var user in users)
                    {
                        var userId = TelegramUtils.GetUserId(user);
                        if (userId == null)
                        {
                            continue;
                        }
                        CreateCachedUserAndAdd(user, Convert.ToUInt32(userId), database);
                    }
                }
            }
        }


        /// <summary>
        /// Adds a single user to the database, if the user already exists it updates the current user
        /// </summary>
        /// <param name="user">the user to be added or updated</param>
        public void AddUser(IUser user)
        {
            lock (_userLock)
            {
                using (var database = new SqlDatabase<CachedUser>(GetDatabasePath(true)))
                {
                    var userIdString = TelegramUtils.GetUserId(user);
                    Utils.DebugPrint("got user id " + userIdString);
                    if (userIdString == null)
                    {
                        return;
                    }
                    var userId = Convert.ToUInt32(userIdString);
                    CreateCachedUserAndAdd(user, userId, database);
                }
            }
        }

        /// <summary>
        /// Adds the users to the current map in memory and serilaizes it
        /// </summary>
        /// <param name="chats">List of IChat objects</param>
        public void AddChats(List<IChat> chats)
        {

            lock (_chatLock)
            {
                using (var database = new SqlDatabase<CachedChat>(GetDatabasePath(false)))
                {
                    foreach (var chat in chats)
                    {
                        var chatId = TelegramUtils.GetChatId(chat);
                        if (chatId == null)
                        {
                            continue;
                        }
                        CreateCachedChatAndAdd(chat, Convert.ToUInt32(chatId), database);
                    }
                }
            }
        }


        /// <summary>
        /// Gets the chat object for the chatid specified
        /// </summary>
        /// <param name="chatId">the chat id of the the object needed</param>
        /// <returns></returns>
        public IChat GetChat(uint chatId)
        {
            lock (_chatLock)
            {
                using (var database = new SqlDatabase<CachedChat>(GetDatabasePath(false)))
                {
                    var chat = database.Store.Where(x => x.Id == chatId).FirstOrDefault();
                    if (chat == null)
                    {
                        return null;
                    }
                    var stream = new MemoryStream(chat.ProtoBufBytes);
                    stream.Position = 0;
                    IChat iChat = Serializer.Deserialize<Chat>(stream);
                    return iChat;
                }
            }
        }

        /// <summary>
        /// Adds a chat or updates it if it already exists
        /// </summary>
        /// <param name="chat">chat to add or update</param>
        public void AddChat(IChat chat)
        {
            lock (_chatLock)
            {
                using (var database = new SqlDatabase<CachedChat>(GetDatabasePath(false)))
                {
                    var chatId = TelegramUtils.GetChatId(chat);
                    if (chatId == null)
                    {
                        return;
                    }
                    CreateCachedChatAndAdd(chat, Convert.ToUInt32(chatId), database);
                }
            }
        }

        public List<IChat> GetAllChats()
        {
            lock (_chatLock)
            {
                List<IChat> chatsToReturn = new List<IChat>();
                using (var database = new SqlDatabase<CachedChat>(GetDatabasePath(false)))
                {
                    var chachedChats = database.Store;
                    foreach (var cachedChat in chachedChats)
                    {
                        var stream = new MemoryStream(cachedChat.ProtoBufBytes);
                        stream.Position = 0;
                        var iChat = Serializer.Deserialize<Chat>(stream);
                        chatsToReturn.Add(iChat);
                    }
                }
                return chatsToReturn;
            }
            
        }


        /// <summary>
        /// Returns true if the databases have been created,
        /// Should be used to check if its the first time this is running, if yes 
        /// Intitalize the database from the data from the server
        /// </summary>
        /// <returns></returns>
        public bool DatabasesExist()
        {
            if (!File.Exists(GetDatabasePath(true)))
            {
                return false;
            }
            using (var database = new SqlDatabase<CachedUser>(GetDatabasePath(true)))
            {
                if (database.Failed)
                {
                    return false;
                }
            }
            return true;
        }

        #region private_methods

        private void CreateCachedUserAndAdd(IUser user, uint userId, SqlDatabase<CachedUser> database)
        {
            var memoryStream = ConvertUserToMemoryStream(user);
            var cachedUser = new CachedUser
            {
                Id = userId,
                ProtoBufBytes = memoryStream.ToArray(),
            };
            var dbUser = database.Store.Where(x => x.Id == userId).FirstOrDefault();
            if (dbUser != null)
            {
                database.Store.Delete(x => x.Id == userId);
                database.Add(cachedUser);
            }
            else
            {
                database.Add(cachedUser);
            }

        }


        private void CreateCachedChatAndAdd(IChat chat, uint chatId, SqlDatabase<CachedChat> database)
        {
            var memoryStream = ConvertChatToMemoryStream(chat);
            var cachedChat = new CachedChat
            {
                Id = chatId,
                ProtoBufBytes = memoryStream.ToArray(),
            };
            var dbUser = database.Store.Where(x => x.Id == chatId).FirstOrDefault();
            if (dbUser != null)
            {
                database.Store.Delete(x => x.Id == chatId);
                database.Add(cachedChat);
            }
            else
            {
                database.Add(cachedChat);
            }

        }

        private MemoryStream ConvertUserToMemoryStream(IUser user)
        {
            MemoryStream memoryStream = new MemoryStream();
            Serializer.Serialize<IUser>(memoryStream, user);
            return memoryStream;
        }

        private MemoryStream ConvertChatToMemoryStream(IChat chat)
        {
            MemoryStream memoryStream = new MemoryStream();
            Serializer.Serialize<IChat>(memoryStream, chat);
            return memoryStream;
        }



        private string GetDatabasePath(bool isUser)
        {
            if (isUser && _userPathCached != null)
            {
                return _userPathCached;
            }
            else if (!isUser && _chatPathCached != null)
            {
                return _chatPathCached;
            }

            var databasePath = Platform.GetDatabasePath();
            if (!Directory.Exists(databasePath))
            {
                Utils.DebugPrint("Creating database directory.");
                Directory.CreateDirectory(databasePath);
            }

            if (isUser)
            {
                var userPath = Path.Combine(databasePath, "userscache.db");
                _userPathCached = userPath;
                return userPath;
            }
            else
            {
                var userChat = Path.Combine(databasePath, "chatscache.db");
                _chatPathCached = userChat;
                return userChat;
            }
        }

        #endregion

    }
}

