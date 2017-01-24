using System.Collections.Generic;
using SQLite;
using ProtoBuf;
using System;

namespace Disa.Framework
{
	[Serializable]
    [ProtoContract]
    public class BubbleGroupCache
    {
        [ProtoMember(1)]
        public string Name { get; set; }
        [ProtoMember(2)]
        public DisaThumbnail Photo { get; set; }
        [ProtoMember(3)]
        public List<DisaParticipant> Participants { get; set; }
        [ProtoMember(4)]
        public string Guid { get; set; }
    }
}