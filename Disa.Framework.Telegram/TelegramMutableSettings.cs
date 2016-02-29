using System;

namespace Disa.Framework.Telegram
{
    public class TelegramMutableSettings : DisaMutableSettings
    {
        public uint Date { get; set; }
        public uint Pts { get; set; }
        public uint Qts { get; set; }
        public uint Seq { get; set; }
    }
}

