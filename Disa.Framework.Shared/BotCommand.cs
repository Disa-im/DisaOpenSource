using System;
using ProtoBuf;
namespace Disa.Framework
{
    [Serializable]
    [ProtoContract]
    public class BotCommand : Mentions
    {
        /// <summary>
        /// Holds the Bot username.
        /// </summary>
        [ProtoMember(1)]
        public string Username { get; set; }
    }
}
