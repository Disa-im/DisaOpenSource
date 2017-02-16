using System.Collections.Generic;

namespace Disa.Framework.Bot
{
    public class BotResults
    {
        public bool Gallery { get; set; }
        public int QueryId { get; set; }
        public string NextOffset { get; set; }
        public InlineBotSwitchPM SwitchPm { get; set; }
        public List<BotInlineResultBase> Results { get; set; }

    }
}
