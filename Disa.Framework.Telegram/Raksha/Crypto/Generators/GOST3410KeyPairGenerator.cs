using System;
using BigMath;
using Raksha.Asn1.CryptoPro;
using Raksha.Crypto.Parameters;
using Raksha.Math;
using Raksha.Security;

namespace Raksha.Crypto.Generators
{
	/**
	 * a GOST3410 key pair generator.
	 * This generates GOST3410 keys in line with the method described
	 * in GOST R 34.10-94.
	 */
	public class Gost3410KeyPairGenerator
		: IAsymmetricCipherKeyPairGenerator
	{
		private Gost3410KeyGenerationParameters param;

		public void Init(
			KeyGenerationParameters parameters)
		{
			if (parameters is Gost3410KeyGenerationParameters)
			{
				this.param = (Gost3410KeyGenerationParameters) parameters;
			}
			else
			{
				Gost3410KeyGenerationParameters kgp = new Gost3410KeyGenerationParameters(
					parameters.Random,
					CryptoProObjectIdentifiers.GostR3410x94CryptoProA);

				if (parameters.Strength != kgp.Parameters.P.BitLength - 1)
				{
					// TODO Should we complain?
				}

				this.param = kgp;
			}
		}

		public AsymmetricCipherKeyPair GenerateKeyPair()
		{
			SecureRandom random = param.Random;
			Gost3410Parameters gost3410Params = param.Parameters;

			BigInteger q = gost3410Params.Q;
			BigInteger x;
			do
			{
				x = new BigInteger(256, random);
			}
			while (x.Sign < 1 || x.CompareTo(q) >= 0);

			BigInteger p = gost3410Params.P;
			BigInteger a = gost3410Params.A;

			// calculate the public key.
			BigInteger y = a.ModPow(x, p);

			if (param.PublicKeyParamSet != null)
			{
				return new AsymmetricCipherKeyPair(
					new Gost3410PublicKeyParameters(y, param.PublicKeyParamSet),
					new Gost3410PrivateKeyParameters(x, param.PublicKeyParamSet));
			}

			return new AsymmetricCipherKeyPair(
				new Gost3410PublicKeyParameters(y, gost3410Params),
				new Gost3410PrivateKeyParameters(x, gost3410Params));
		}
	}
}
