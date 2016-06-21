using System;
using BigMath;
using Raksha.Asn1;
using Raksha.Asn1.X509;
using Raksha.Math;

namespace Raksha.Asn1.Cms
{
    public class IssuerAndSerialNumber
        : Asn1Encodable
    {
        private X509Name	name;
        private DerInteger	serialNumber;

        public static IssuerAndSerialNumber GetInstance(
            object obj)
        {
            if (obj is IssuerAndSerialNumber)
                return (IssuerAndSerialNumber)obj;

			if (obj is Asn1Sequence)
                return new IssuerAndSerialNumber((Asn1Sequence)obj);

			throw new ArgumentException(
                "Illegal object in IssuerAndSerialNumber: " + obj.GetType().Name);
        }

        public IssuerAndSerialNumber(
            Asn1Sequence seq)
        {
            this.name = X509Name.GetInstance(seq[0]);
            this.serialNumber = (DerInteger) seq[1];
        }

		public IssuerAndSerialNumber(
            X509Name	name,
            BigInteger	serialNumber)
        {
            this.name = name;
            this.serialNumber = new DerInteger(serialNumber);
        }

        public IssuerAndSerialNumber(
            X509Name	name,
            DerInteger	serialNumber)
        {
            this.name = name;
            this.serialNumber = serialNumber;
        }

		public X509Name Name
		{
			get { return name; }
		}

		public DerInteger SerialNumber
		{
			get { return serialNumber; }
		}

		public override Asn1Object ToAsn1Object()
        {
			return new DerSequence(name, serialNumber);
        }
    }
}
