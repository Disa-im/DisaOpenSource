using System;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public class TelegramContact : Disa.Framework.Contact
    {
        public TelegramContact()
        {
            
        }

        public User User { get; set; }
    }

    public class TelegramPartyContact : Disa.Framework.PartyContact
    {
        public TelegramPartyContact()
        {

        }

        /// <summary>
        /// An AccessHash is used by Telegram when accessing public entities such as public channels.
        /// </summary>
        public ulong AccessHash { get; set; }
    }

    public class TelegramBotContact : Disa.Framework.BotContact
    {
        public TelegramBotContact()
        {

        }


        public User User { get; set; }
    }

}

