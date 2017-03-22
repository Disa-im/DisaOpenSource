using ProtoBuf;
using System;
namespace Disa.Framework.Bots
{
    [Serializable]
    [ProtoContract]
    public class BotCommand
    {
        [ProtoMember(1)]
        public string Command { get; set; }

        [ProtoMember(2)]
        public string Description { get; set; }
    }
}
