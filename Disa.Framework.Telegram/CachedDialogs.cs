using System;
using SharpTelegram.Schema;
using ProtoBuf;

namespace Disa.Framework.Telegram
{
    [ProtoContract]
    public class CachedDialogs
    {
        public CachedDialogs()
        {
            Dialogs = new ThreadSafeList<IDialog>();
            Messages = new ThreadSafeList<IMessage>();
            Chats = new ThreadSafeList<IChat>();
            Users = new ThreadSafeList<IUser>();
            FullChats = new ThreadSafeList<IMessagesChatFull>();
            FullChatFailures = new ThreadSafeList<string>();
        }

        // TODO: remove this. It's not needed anymore with Disa's implemetation.
        // We only need to cache Chats, Users, and FullChats
        public ThreadSafeList<IDialog> Dialogs { get; private set; }

        //TODO: remove this.
        public ThreadSafeList<IMessage> Messages { get; private set; }

        [ProtoMember(1)]
        public ThreadSafeList<IChat> Chats { get; private set; }

        [ProtoMember(2)]
        public ThreadSafeList<IUser> Users { get; private set; }

        public ThreadSafeList<IMessagesChatFull> FullChats { get; private set; }

        public ThreadSafeList<string> FullChatFailures { get; private set; }
    }
}

