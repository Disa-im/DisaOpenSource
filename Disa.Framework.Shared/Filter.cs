using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework
{
    [ProtoContract]
    public class Filter
    {
        [ProtoMember(1)]
        public string Name { get; set; }
        [ProtoMember(2)]
        public IList<Tag> Tags { get; set; } = new List<Tag>();

        public override bool Equals(object obj)
        {
            var view = obj as Filter;
            if (view == null)
            {
                return false;
            }
            return Name.Equals(view.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
