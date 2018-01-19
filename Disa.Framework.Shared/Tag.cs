using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class Tag
    {
        /// <summary>
        /// To be used by plugin, should NOT be used by framework
        /// </summary>
        [ProtoMember(1)]
        public string Id { get; set; }
        /// <summary>
        /// To be used by framework
        /// </summary>
        [ProtoMember(2)]
        internal string FullyQualifiedId { get; set; }
        [ProtoMember(3)]
        public string Name { get; set; }
        
        /// <summary>
        /// A name that has been assigned for the UI
        /// </summary>
        public string ConvenientName { get; set; }

        [ProtoMember(4, AsReference = true)]
        public Tag Parent { get; set; }
        
        public string ServiceName
        {
            get => Service?.Information.ServiceName;
        }

        public HashSet<string> BubbleGroupAddresses { get; set; } = new HashSet<string>();

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
