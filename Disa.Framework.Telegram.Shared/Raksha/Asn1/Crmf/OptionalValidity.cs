﻿using System;
using Raksha.Asn1.X509;

namespace Raksha.Asn1.Crmf
{
    public class OptionalValidity
        : Asn1Encodable
    {
        private readonly Time notBefore;
        private readonly Time notAfter;

        private OptionalValidity(Asn1Sequence seq)
        {
            foreach (Asn1TaggedObject tObj in seq)
            {
                if (tObj.TagNo == 0)
                {
                    notBefore = Time.GetInstance(tObj, true);
                }
                else
                {
                    notAfter = Time.GetInstance(tObj, true);
                }
            }
        }

        public static OptionalValidity GetInstance(object obj)
        {
            if (obj is OptionalValidity)
                return (OptionalValidity)obj;

            if (obj is Asn1Sequence)
                return new OptionalValidity((Asn1Sequence)obj);

            throw new ArgumentException("Invalid object: " + obj.GetType().Name, "obj");
        }

        /**
         * <pre>
         * OptionalValidity ::= SEQUENCE {
         *                        notBefore  [0] Time OPTIONAL,
         *                        notAfter   [1] Time OPTIONAL } --at least one MUST be present
         * </pre>
         * @return a basic ASN.1 object representation.
         */
        public override Asn1Object ToAsn1Object()
        {
            Asn1EncodableVector v = new Asn1EncodableVector();

            if (notBefore != null)
            {
                v.Add(new DerTaggedObject(true, 0, notBefore));
            }

            if (notAfter != null)
            {
                v.Add(new DerTaggedObject(true, 1, notAfter));
            }

            return new DerSequence(v);
        }
    }
}
