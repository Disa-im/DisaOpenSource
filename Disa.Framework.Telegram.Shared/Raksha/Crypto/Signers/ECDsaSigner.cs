using System;
using BigMath;
using Raksha.Crypto;
using Raksha.Crypto.Digests;
using Raksha.Crypto.Parameters;
using Raksha.Math;
using Raksha.Math.EC;
using Raksha.Security;

namespace Raksha.Crypto.Signers
{
	/**
	 * EC-DSA as described in X9.62
	 */
	public class ECDsaSigner
		: IDsa
	{
		private ECKeyParameters key;
		private SecureRandom random;

		public string AlgorithmName
		{
			get { return "ECDSA"; }
		}

		public void Init(
			bool				forSigning,
			ICipherParameters	parameters)
		{
			if (forSigning)
			{
				if (parameters is ParametersWithRandom)
				{
					ParametersWithRandom rParam = (ParametersWithRandom) parameters;

					this.random = rParam.Random;
					parameters = rParam.Parameters;
				}
				else
				{
					this.random = new SecureRandom();
				}

				if (!(parameters is ECPrivateKeyParameters))
					throw new InvalidKeyException("EC private key required for signing");

				this.key = (ECPrivateKeyParameters) parameters;
			}
			else
			{
				if (!(parameters is ECPublicKeyParameters))
					throw new InvalidKeyException("EC public key required for verification");

				this.key = (ECPublicKeyParameters) parameters;
			}
		}

		// 5.3 pg 28
		/**
		 * Generate a signature for the given message using the key we were
		 * initialised with. For conventional DSA the message should be a SHA-1
		 * hash of the message of interest.
		 *
		 * @param message the message that will be verified later.
		 */
		public BigInteger[] GenerateSignature(
			byte[] message)
		{
			BigInteger n = key.Parameters.N;
			BigInteger e = calculateE(n, message);

			BigInteger r = null;
			BigInteger s = null;

			// 5.3.2
			do // Generate s
			{
				BigInteger k = null;

				do // Generate r
				{
					do
					{
						k = new BigInteger(n.BitLength, random);
					}
					while (k.Sign == 0 || k.CompareTo(n) >= 0);

					ECPoint p = key.Parameters.G.Multiply(k);

					// 5.3.3
					BigInteger x = p.X.ToBigInteger();

					r = x.Mod(n);
				}
				while (r.Sign == 0);

				BigInteger d = ((ECPrivateKeyParameters)key).D;

				s = k.ModInverse(n).Multiply(e.Add(d.Multiply(r))).Mod(n);
			}
			while (s.Sign == 0);

			return new BigInteger[]{ r, s };
		}

		// 5.4 pg 29
		/**
		 * return true if the value r and s represent a DSA signature for
		 * the passed in message (for standard DSA the message should be
		 * a SHA-1 hash of the real message to be verified).
		 */
		public bool VerifySignature(
			byte[]		message,
			BigInteger	r,
			BigInteger	s)
		{
			BigInteger n = key.Parameters.N;

			// r and s should both in the range [1,n-1]
			if (r.Sign < 1 || s.Sign < 1
				|| r.CompareTo(n) >= 0 || s.CompareTo(n) >= 0)
			{
				return false;
			}

			BigInteger e = calculateE(n, message);
			BigInteger c = s.ModInverse(n);

			BigInteger u1 = e.Multiply(c).Mod(n);
			BigInteger u2 = r.Multiply(c).Mod(n);

			ECPoint G = key.Parameters.G;
			ECPoint Q = ((ECPublicKeyParameters) key).Q;

			ECPoint point = ECAlgorithms.SumOfTwoMultiplies(G, u1, Q, u2);

			BigInteger v = point.X.ToBigInteger().Mod(n);

			return v.Equals(r);
		}

		private BigInteger calculateE(
			BigInteger	n,
			byte[]		message)
		{
			int messageBitLength = message.Length * 8;
			BigInteger trunc = new BigInteger(1, message);

			if (n.BitLength < messageBitLength)
			{
				trunc = trunc.ShiftRight(messageBitLength - n.BitLength);
			}

			return trunc;
		}
	}
}
