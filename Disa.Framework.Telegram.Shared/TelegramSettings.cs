using System;

namespace Disa.Framework.Telegram
{
    public class TelegramSettings : DisaSettings
    {
        public byte[] AuthKey { get; set; }
        public ulong Salt { get; set; }
        public uint NearestDcId { get; set; }
        public string NearestDcIp { get; set; }
        public int NearestDcPort { get; set; }
        public uint AccountId { get; set; }
    }
}

