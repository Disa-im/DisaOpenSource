using System;
using SharpTelegram.Schema.Layer18;

namespace Disa.Framework.Telegram
{
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

        public ThreadSafeList<IDialog> Dialogs { get; private set; }

        public ThreadSafeList<IMessage> Messages { get; private set; }

        public ThreadSafeList<IChat> Chats { get; private set; }

        public ThreadSafeList<IUser> Users { get; private set; }

        public ThreadSafeList<IMessagesChatFull> FullChats { get; private set; }

        public ThreadSafeList<string> FullChatFailures { get; private set; }
    }
}

