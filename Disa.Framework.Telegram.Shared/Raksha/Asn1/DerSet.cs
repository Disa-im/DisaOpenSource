// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DerSet.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Raksha.Asn1
{
    /**
	 * A Der encoded set object
	 */

    public class DerSet : Asn1Set
    {
        public static readonly DerSet Empty = new DerSet();

        /**
		 * create an empty set
		 */

        public DerSet() : base(0)
        {
        }

        /**
		 * @param obj - a single object that makes up the set.
		 */

        public DerSet(Asn1Encodable obj) : base(1)
        {
            AddObject(obj);
        }

        public DerSet(params Asn1Encodable[] v) : base(v.Length)
        {
            foreach (Asn1Encodable o in v)
            {
                AddObject(o);
            }

            Sort();
        }

        /**
		 * @param v - a vector of objects making up the set.
		 */

        public DerSet(Asn1EncodableVector v) : this(v, true)
        {
        }

        internal DerSet(Asn1EncodableVector v, bool needsSorting) : base(v.Count)
        {
            foreach (Asn1Encodable o in v)
            {
                AddObject(o);
            }

            if (needsSorting)
            {
                Sort();
            }
        }

        public static DerSet FromVector(Asn1EncodableVector v)
        {
            return v.Count < 1 ? Empty : new DerSet(v);
        }

        internal static DerSet FromVector(Asn1EncodableVector v, bool needsSorting)
        {
            return v.Count < 1 ? Empty : new DerSet(v, needsSorting);
        }

        /*
		 * A note on the implementation:
		 * <p>
		 * As Der requires the constructed, definite-length model to
		 * be used for structured types, this varies slightly from the
		 * ASN.1 descriptions given. Rather than just outputing Set,
		 * we also have to specify Constructed, and the objects length.
		 */

        internal override void Encode(DerOutputStream derOut)
        {
            // TODO Intermediate buffer could be avoided if we could calculate expected length
            using (var bOut = new MemoryStream())
            {
                using (var dOut = new DerOutputStream(bOut))
                {
                    foreach (Asn1Encodable obj in this)
                    {
                        dOut.WriteObject(obj);
                    }

                    dOut.Dispose();

                    byte[] bytes = bOut.ToArray();

                    derOut.WriteEncoded(Asn1Tags.Set | Asn1Tags.Constructed, bytes);
                }
            }
        }
    }
}
