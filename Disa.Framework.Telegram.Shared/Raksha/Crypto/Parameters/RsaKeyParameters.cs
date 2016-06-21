using System;
using BigMath;
using Raksha.Crypto;
using Raksha.Math;

namespace Raksha.Crypto.Parameters
{
	public class RsaKeyParameters
		: AsymmetricKeyParameter
    {
        private readonly BigInteger modulus;
        private readonly BigInteger exponent;

		public RsaKeyParameters(
            bool		isPrivate,
            BigInteger	modulus,
            BigInteger	exponent)
			: base(isPrivate)
        {
			if (modulus == null)
				throw new ArgumentNullException("modulus");
			if (exponent == null)
				throw new ArgumentNullException("exponent");
			if (modulus.Sign <= 0)
				throw new ArgumentException("Not a valid RSA modulus", "modulus");
			if (exponent.Sign <= 0)
				throw new ArgumentException("Not a valid RSA exponent", "exponent");

			this.modulus = modulus;
			this.exponent = exponent;
        }

		public BigInteger Modulus
        {
            get { return modulus; }
        }

		public BigInteger Exponent
        {
            get { return exponent; }
        }

		public override bool Equals(
			object obj)
        {
            RsaKeyParameters kp = obj as RsaKeyParameters;

			if (kp == null)
			{
				return false;
			}

			return kp.IsPrivate == this.IsPrivate
				&& kp.Modulus.Equals(this.modulus)
				&& kp.Exponent.Equals(this.exponent);
        }

		public override int GetHashCode()
        {
            return modulus.GetHashCode() ^ exponent.GetHashCode() ^ IsPrivate.GetHashCode();
        }
    }
}
