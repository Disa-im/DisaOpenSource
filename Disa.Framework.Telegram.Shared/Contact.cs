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
    }
}

