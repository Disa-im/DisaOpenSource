using System;
using SQLite;

namespace Disa.Framework.Telegram
{
    public class CachedThumbnail
    {
        [Indexed]
        public string Id { get; set; }

        public byte[] ThumbnailBytes { get; set; }
    }
}

