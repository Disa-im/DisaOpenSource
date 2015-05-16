using System;
using BigMath;
using Raksha.Asn1;
using Raksha.Math;

namespace Raksha.Crypto.Parameters
{
	public class Gost3410PublicKeyParameters
		: Gost3410KeyParameters
	{
		private readonly BigInteger y;

		public Gost3410PublicKeyParameters(
			BigInteger y,
			Gost3410Parameters parameters)
			: base(false, parameters)
		{
			if (y.Sign < 1 || y.CompareTo(Parameters.P) >= 0)
				throw new ArgumentException("Invalid y for GOST3410 public key", "y");

			this.y = y;
		}

		public Gost3410PublicKeyParameters(
			BigInteger			y,
			DerObjectIdentifier publicKeyParamSet)
			: base(false, publicKeyParamSet)
		{
			if (y.Sign < 1 || y.CompareTo(Parameters.P) >= 0)
				throw new ArgumentException("Invalid y for GOST3410 public key", "y");

			this.y = y;
		}

		public BigInteger Y
		{
			get { return y; }
		}
	}
}
