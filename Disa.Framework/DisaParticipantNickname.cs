using System;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    internal class DisaParticipantNickname
    {
        [ProtoMember(1)]
        public string Address { get; set; }
        [ProtoMember(2)]
        public string Nickname { get; set; }
    }
}

