using System;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class DisaQuotedTitle
    {
        [ProtoMember(1)]
        public string Address { get; set; }
        [ProtoMember(2)]
        public string Title { get; set; }
    }
}

