using System;
using SQLite;


namespace Disa.Framework.Telegram
{
    public class CachedChat
    {
        [Indexed]
        public uint Id { get; set; }

        public byte[] ProtoBufBytes { get; set; }

        public override string ToString()
        {
            return "Id " + Id + " bytes " + ProtoBufBytes;
        }
    }
}

