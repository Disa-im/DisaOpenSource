using System;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class DisaReadTime
    {
        [NonSerialized]
        public object Tag;
        [ProtoMember(1)]
        public string ParticipantAddress { get; set; }
        [ProtoMember(2)]
        public long Time { get; set; }
    }
}

