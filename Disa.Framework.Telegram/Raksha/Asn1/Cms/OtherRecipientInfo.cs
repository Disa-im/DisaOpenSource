using System;

using Raksha.Asn1;

namespace Raksha.Asn1.Cms
{
    public class OtherRecipientInfo
        : Asn1Encodable
    {
        private DerObjectIdentifier	oriType;
        private Asn1Encodable		oriValue;

		public OtherRecipientInfo(
            DerObjectIdentifier	oriType,
            Asn1Encodable		oriValue)
        {
            this.oriType = oriType;
            this.oriValue = oriValue;
        }

		public OtherRecipientInfo(
            Asn1Sequence seq)
        {
            oriType = DerObjectIdentifier.GetInstance(seq[0]);
            oriValue = seq[1];
		}

		/**
         * return a OtherRecipientInfo object from a tagged object.
         *
         * @param obj the tagged object holding the object we want.
         * @param explicitly true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception ArgumentException if the object held by the
         *          tagged object cannot be converted.
         */
        public static OtherRecipientInfo GetInstance(
            Asn1TaggedObject	obj,
            bool				explicitly)
        {
            return GetInstance(Asn1Sequence.GetInstance(obj, explicitly));
        }

		/**
         * return a OtherRecipientInfo object from the given object.
         *
         * @param obj the object we want converted.
         * @exception ArgumentException if the object cannot be converted.
         */
        public static OtherRecipientInfo GetInstance(
            object obj)
        {
            if (obj == null || obj is OtherRecipientInfo)
                return (OtherRecipientInfo)obj;

			if (obj is Asn1Sequence)
                return new OtherRecipientInfo((Asn1Sequence)obj);

			throw new ArgumentException("Invalid OtherRecipientInfo: " + obj.GetType().Name);
        }

		public DerObjectIdentifier OriType
		{
			get { return oriType; }
		}

		public Asn1Encodable OriValue
		{
			get { return oriValue; }
		}

		/**
         * Produce an object suitable for an Asn1OutputStream.
         * <pre>
         * OtherRecipientInfo ::= Sequence {
         *    oriType OBJECT IDENTIFIER,
         *    oriValue ANY DEFINED BY oriType }
         * </pre>
         */
        public override Asn1Object ToAsn1Object()
        {
			return new DerSequence(oriType, oriValue);
        }
    }
}
