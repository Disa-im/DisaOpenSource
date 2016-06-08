using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        /// chat dictionary for holding the chats as per the id and the object
        /// </summary>
        private ConcurrentDictionary<uint, IChat> _chatDictionary;

        /// <summary>
        /// Lock for concurrent accesses to the user sql database
        /// </summary>
        private readonly object _userLock;

        /// <summary>
        /// boolean indicating chats have been loaded before
        /// </summary>
        private bool _chatsLoaded;


        /// <summary>
        /// Default constructor
        /// </summary>
        public CachedDialogs()
        {
            _chatDictionary = new ConcurrentDictionary<uint, IChat>();

            _userLock = new object();
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
                    if (userIdString == null)
                    {
                        return;
                    }
                    var userId = Convert.ToUInt32(userIdString);
                    var dbUser = database.Store.Where(x => x.Id == userId).FirstOrDefault();
                    //if there is an existing user, delete it and add it again
                    if (dbUser != null)
                    {
                        database.Store.Delete(x => x.Id == userId);
                        CreateCachedUserAndAdd(user, userId, database);
                    }
                    else
                    {
                        CreateCachedUserAndAdd(user, userId, database);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the users to the current map in memory and serilaizes it
        /// </summary>
        /// <param name="chats">List of IChat objects</param>
        public void AddChats(List<IChat> chats)
        {
            foreach (var chat in chats)
            {
                var chatId = TelegramUtils.GetChatId(chat);
                if (chatId == null)
                {
                    continue;
                }
                var chatIdInt = Convert.ToUInt32(chatId);
                _chatDictionary[chatIdInt] = chat;
            }
            SaveChats();
            _chatsLoaded = true; //since this is the first time we are adding chats anyway
        }


        /// <summary>
        /// Gets the chat object for the chatid specified
        /// </summary>
        /// <param name="chatId">the chat id of the the object needed</param>
        /// <returns></returns>
        public IChat GetChat(uint chatId)
        {
            if (!_chatsLoaded)
            {
                LoadChats();
                _chatsLoaded = true;
            }
            IChat iChat;
            _chatDictionary.TryGetValue(chatId, out iChat);
            return iChat;
        }

        /// <summary>
        /// Adds a chat or updates it if it already exists
        /// </summary>
        /// <param name="chat">chat to add or update</param>
        public void AddChat(IChat chat)
        {
            if (chat == null)
            {
                return;
            }
            var chatId = Convert.ToUInt32(TelegramUtils.GetChatId(chat));
            _chatDictionary[chatId] = chat;
            SaveChats();
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
            var memoryStream = ConvertUserToMemeoryStream(user);
            var cachedUser = new CachedUser
            {
                Id = userId,
                ProtoBufBytes = memoryStream.ToArray(),
            };
            database.Add(cachedUser);
        }

        private MemoryStream ConvertUserToMemeoryStream(IUser user)
        {
            MemoryStream memoryStream = new MemoryStream();
            Serializer.Serialize<IUser>(memoryStream, user);
            return memoryStream;
        }

        private void LoadChats()
        {
            using (var file = File.OpenRead(GetDatabasePath(false)))
            {
                _chatDictionary = Serializer.Deserialize<ConcurrentDictionary<uint, IChat>>(file);
            }
        }

        private void SaveChats()
        {
            using (var file = File.Create(GetDatabasePath(false)))
            {
                Serializer.Serialize(file, _chatDictionary);
            }
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

