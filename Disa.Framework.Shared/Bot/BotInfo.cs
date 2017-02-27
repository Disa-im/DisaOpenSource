using System.Collections.Generic;

namespace Disa.Framework.Bot
{
    public class BotInfo
    {
        public int UserId { get; set; }
        public string Description { get; set; }
        public List<BotCommand> Commands { get; set; }
        public int Version { get; set; }
    }
}
