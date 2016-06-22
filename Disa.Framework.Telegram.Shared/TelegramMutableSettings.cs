using System;

namespace Disa.Framework.Telegram
{
    public class TelegramMutableSettings : DisaMutableSettings
    {
        public uint Date { get; set; }
        public uint Pts { get; set; }
        public uint Qts { get; set; }
        public uint Seq { get; set; } //TODO: needed? Remove from all of Telegram code (including State object, SaveState method, etc.)
    }
}

