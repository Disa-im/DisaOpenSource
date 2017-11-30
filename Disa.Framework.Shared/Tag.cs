using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class Tag
    {
        [ProtoMember(1)]
        public string Id { get; set; }
        [ProtoMember(2)]
        internal string FullyQualifiedId { get; set; }
        [ProtoMember(3)]
        public string Name { get; set; }
        [ProtoMember(4, AsReference = true)]
        public Tag Parent { get; set; }

        [ProtoMember(5)]
        public string ServiceName { get; set; }

        public Service Service { get; set; }
        
        public override int GetHashCode()
        {
            return FullyQualifiedId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var tag = obj as Tag;
            if (tag == null)
            {
                return false;
            }
            return FullyQualifiedId.Equals(tag.FullyQualifiedId);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
