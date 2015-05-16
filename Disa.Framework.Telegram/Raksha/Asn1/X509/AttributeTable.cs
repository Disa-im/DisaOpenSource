// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AttributeTable.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using Raksha.Utilities;

namespace Raksha.Asn1.X509
{
    public class AttributeTable
    {
        private readonly IDictionary _attributes;

        public AttributeTable(IDictionary attrs)
        {
            _attributes = Platform.CreateHashtable(attrs);
        }

        public AttributeTable(Asn1EncodableVector v)
        {
            _attributes = Platform.CreateHashtable(v.Count);

            for (int i = 0; i != v.Count; i++)
            {
                AttributeX509 a = AttributeX509.GetInstance(v[i]);

                _attributes.Add(a.AttrType, a);
            }
        }

        public AttributeTable(Asn1Set s)
        {
            _attributes = Platform.CreateHashtable(s.Count);

            for (int i = 0; i != s.Count; i++)
            {
                AttributeX509 a = AttributeX509.GetInstance(s[i]);

                _attributes.Add(a.AttrType, a);
            }
        }

        public AttributeX509 Get(DerObjectIdentifier oid)
        {
            return (AttributeX509) _attributes[oid];
        }

        public IDictionary ToDictionary()
        {
            return Platform.CreateHashtable(_attributes);
        }
    }
}
