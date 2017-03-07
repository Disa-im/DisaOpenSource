using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public class MessagesBotResults
    {
        public bool Gallery { get; set; }

        public int QueryId { get; set; }

        public string NextOffset { get; set; }

        public InlineBotSwitchPM SwitchPm { get; set; }

        public List<BotInlineResult> Results { get; set; }
    }
}
