using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Disa.Framework.Bots
{
    [Serializable]
    [ProtoContract]
    public class BotInfo
    {
        [ProtoMember(1)]
        public string Address { get; set; }

        [ProtoMember(2)]
        public string Description { get; set; }

        [ProtoMember(3)]
        public List<BotCommand> Commands { get; set; }
    }
}
