// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AttributeTable.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using Raksha.Utilities;

namespace Raksha.Asn1.Cms
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

            foreach (Asn1Encodable o in v)
            {
                Attribute a = Attribute.GetInstance(o);

                AddAttribute(a);
            }
        }

        public AttributeTable(Asn1Set s)
        {
            _attributes = Platform.CreateHashtable(s.Count);

            for (int i = 0; i != s.Count; i++)
            {
                Attribute a = Attribute.GetInstance(s[i]);

                AddAttribute(a);
            }
        }

        public AttributeTable(Attributes attrs) : this(Asn1Set.GetInstance(attrs.ToAsn1Object()))
        {
        }

        /// <summary>Return the first attribute matching the given OBJECT IDENTIFIER</summary>
        public Attribute this[DerObjectIdentifier oid]
        {
            get
            {
                object obj = _attributes[oid];

                if (obj is IList)
                {
                    return (Attribute) ((IList) obj)[0];
                }

                return (Attribute) obj;
            }
        }

        public int Count
        {
            get
            {
                int total = 0;

                foreach (object o in _attributes.Values)
                {
                    if (o is IList)
                    {
                        total += ((IList) o).Count;
                    }
                    else
                    {
                        ++total;
                    }
                }

                return total;
            }
        }

        private void AddAttribute(Attribute a)
        {
            DerObjectIdentifier oid = a.AttrType;
            object obj = _attributes[oid];

            if (obj == null)
            {
                _attributes[oid] = a;
            }
            else
            {
                IList v;

                if (obj is Attribute)
                {
                    v = Platform.CreateArrayList();

                    v.Add(obj);
                    v.Add(a);
                }
                else
                {
                    v = (IList) obj;

                    v.Add(a);
                }

                _attributes[oid] = v;
            }
        }

        /**
        * Return all the attributes matching the OBJECT IDENTIFIER oid. The vector will be
        * empty if there are no attributes of the required type present.
        *
        * @param oid type of attribute required.
        * @return a vector of all the attributes found of type oid.
        */
        public Asn1EncodableVector GetAll(DerObjectIdentifier oid)
        {
            var v = new Asn1EncodableVector();

            object obj = _attributes[oid];

            if (obj is IList)
            {
                foreach (Attribute a in (IList) obj)
                {
                    v.Add(a);
                }
            }
            else if (obj != null)
            {
                v.Add((Attribute) obj);
            }

            return v;
        }

        public IDictionary ToDictionary()
        {
            return Platform.CreateHashtable(_attributes);
        }

        public Asn1EncodableVector ToAsn1EncodableVector()
        {
            var v = new Asn1EncodableVector();

            foreach (object obj in _attributes.Values)
            {
                if (obj is IList)
                {
                    foreach (object el in (IList) obj)
                    {
                        v.Add(Attribute.GetInstance(el));
                    }
                }
                else
                {
                    v.Add(Attribute.GetInstance(obj));
                }
            }

            return v;
        }

        public Attributes ToAttributes()
        {
            return new Attributes(ToAsn1EncodableVector());
        }

        /**
		 * Return a new table with the passed in attribute added.
		 *
		 * @param attrType
		 * @param attrValue
		 * @return
		 */

        public AttributeTable Add(DerObjectIdentifier attrType, Asn1Encodable attrValue)
        {
            var newTable = new AttributeTable(_attributes);

            newTable.AddAttribute(new Attribute(attrType, new DerSet(attrValue)));

            return newTable;
        }

        public AttributeTable Remove(DerObjectIdentifier attrType)
        {
            var newTable = new AttributeTable(_attributes);

            newTable._attributes.Remove(attrType);

            return newTable;
        }
    }
}
