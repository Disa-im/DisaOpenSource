using System;
using Disa.Framework.Mobile;

namespace Disa.Framework.Telegram.Mobile
{
    public class Description : IPluginDescription<Telegram>
    {
        public string FetchDescription(Telegram service)
        {
            return Localize.GetString("TelegramDescription");
        }
    }
}

