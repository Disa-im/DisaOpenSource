// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DerSequence.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Raksha.Asn1
{
    public class DerSequence : Asn1Sequence
    {
        public static readonly DerSequence Empty = new DerSequence();

        /**
		 * create an empty sequence
		 */

        public DerSequence() : base(0)
        {
        }

        /**
		 * create a sequence containing one object
		 */

        public DerSequence(Asn1Encodable obj) : base(1)
        {
            AddObject(obj);
        }

        public DerSequence(params Asn1Encodable[] v) : base(v.Length)
        {
            foreach (Asn1Encodable ae in v)
            {
                AddObject(ae);
            }
        }

        /**
		 * create a sequence containing a vector of objects.
		 */

        public DerSequence(Asn1EncodableVector v) : base(v.Count)
        {
            foreach (Asn1Encodable ae in v)
            {
                AddObject(ae);
            }
        }

        public static DerSequence FromVector(Asn1EncodableVector v)
        {
            return v.Count < 1 ? Empty : new DerSequence(v);
        }

        /*
		 * A note on the implementation:
		 * <p>
		 * As Der requires the constructed, definite-length model to
		 * be used for structured types, this varies slightly from the
		 * ASN.1 descriptions given. Rather than just outputing Sequence,
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

                    derOut.WriteEncoded(Asn1Tags.Sequence | Asn1Tags.Constructed, bytes);
                }
            }
        }
    }
}
