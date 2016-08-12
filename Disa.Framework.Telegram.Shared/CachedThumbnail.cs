using System;
using SQLite;

namespace Disa.Framework.Telegram
{
    public class CachedThumbnail
    {
        [Indexed]
        public string Id { get; set; }

        public byte[] ThumbnailBytes { get; set; }

        public override string ToString()
        {
            return "Id " + Id + " bytes " + ThumbnailBytes;
        }
    }
}

