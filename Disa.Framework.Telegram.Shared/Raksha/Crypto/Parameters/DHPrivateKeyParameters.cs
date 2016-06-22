using System;
using BigMath;
using Raksha.Asn1;
using Raksha.Math;

namespace Raksha.Crypto.Parameters
{
    public class DHPrivateKeyParameters
		: DHKeyParameters
    {
        private readonly BigInteger x;

		public DHPrivateKeyParameters(
            BigInteger		x,
            DHParameters	parameters)
			: base(true, parameters)
        {
            this.x = x;
        }

		public DHPrivateKeyParameters(
            BigInteger			x,
            DHParameters		parameters,
		    DerObjectIdentifier	algorithmOid)
			: base(true, parameters, algorithmOid)
        {
            this.x = x;
        }

		public BigInteger X
        {
            get { return x; }
        }

		public override bool Equals(
			object obj)
        {
			if (obj == this)
				return true;

			DHPrivateKeyParameters other = obj as DHPrivateKeyParameters;

			if (other == null)
				return false;

			return Equals(other);
        }

		protected bool Equals(
			DHPrivateKeyParameters other)
		{
			return x.Equals(other.x) && base.Equals(other);
		}

		public override int GetHashCode()
        {
            return x.GetHashCode() ^ base.GetHashCode();
        }
    }
}
