using System;
using System.Globalization;
using BigMath;
using Raksha.Crypto;
using Raksha.Asn1;
using Raksha.Asn1.Nist;
using Raksha.Asn1.Sec;
using Raksha.Asn1.TeleTrust;
using Raksha.Asn1.X9;
using Raksha.Crypto.Parameters;
using Raksha.Math;
using Raksha.Math.EC;
using Raksha.Security;

namespace Raksha.Crypto.Generators
{
    public class ECKeyPairGenerator
		: IAsymmetricCipherKeyPairGenerator
    {
		private readonly string algorithm;

		private ECDomainParameters parameters;
		private DerObjectIdentifier publicKeyParamSet;
        private SecureRandom random;

		public ECKeyPairGenerator()
			: this("EC")
		{
		}

		public ECKeyPairGenerator(
			string algorithm)
		{
			if (algorithm == null)
				throw new ArgumentNullException("algorithm");

			this.algorithm = VerifyAlgorithmName(algorithm);
		}

		public void Init(
            KeyGenerationParameters parameters)
        {
			if (parameters is ECKeyGenerationParameters)
			{
				ECKeyGenerationParameters ecP = (ECKeyGenerationParameters) parameters;

				this.publicKeyParamSet = ecP.PublicKeyParamSet;
				this.parameters = ecP.DomainParameters;
			}
			else
			{
				DerObjectIdentifier oid;
				switch (parameters.Strength)
				{
					case 192:
						oid = X9ObjectIdentifiers.Prime192v1;
						break;
					case 224:
						oid = SecObjectIdentifiers.SecP224r1;
						break;
					case 239:
						oid = X9ObjectIdentifiers.Prime239v1;
						break;
					case 256:
						oid = X9ObjectIdentifiers.Prime256v1;
						break;
					case 384:
						oid = SecObjectIdentifiers.SecP384r1;
						break;
					case 521:
						oid = SecObjectIdentifiers.SecP521r1;
						break;
					default:
						throw new InvalidParameterException("unknown key size.");
				}

				X9ECParameters ecps = FindECCurveByOid(oid);

				this.parameters = new ECDomainParameters(
					ecps.Curve, ecps.G, ecps.N, ecps.H, ecps.GetSeed());
			}

			this.random = parameters.Random;
		}

		/**
         * Given the domain parameters this routine Generates an EC key
         * pair in accordance with X9.62 section 5.2.1 pages 26, 27.
         */
        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
            BigInteger n = parameters.N;
            BigInteger d;

            do
            {
                d = new BigInteger(n.BitLength, random);
            }
            while (d.Sign == 0 || (d.CompareTo(n) >= 0));

            ECPoint q = parameters.G.Multiply(d);

			if (publicKeyParamSet != null)
			{
				return new AsymmetricCipherKeyPair(
					new ECPublicKeyParameters(algorithm, q, publicKeyParamSet),
					new ECPrivateKeyParameters(algorithm, d, publicKeyParamSet));
			}

			return new AsymmetricCipherKeyPair(
				new ECPublicKeyParameters(algorithm, q, parameters),
				new ECPrivateKeyParameters(algorithm, d, parameters));
		}

		private string VerifyAlgorithmName(
			string algorithm)
		{
			string upper = algorithm.ToUpperInvariant();

			switch (upper)
			{
				case "EC":
				case "ECDSA":
				case "ECDH":
				case "ECDHC":
				case "ECGOST3410":
				case "ECMQV":
					break;
				default:
					throw new ArgumentException("unrecognised algorithm: " + algorithm, "algorithm");
			}

			return upper;
		}

		internal static X9ECParameters FindECCurveByOid(DerObjectIdentifier oid)
		{
			// TODO ECGost3410NamedCurves support (returns ECDomainParameters though)

			X9ECParameters ecP = X962NamedCurves.GetByOid(oid);

			if (ecP == null)
			{
				ecP = SecNamedCurves.GetByOid(oid);

				if (ecP == null)
				{
					ecP = NistNamedCurves.GetByOid(oid);

					if (ecP == null)
					{
						ecP = TeleTrusTNamedCurves.GetByOid(oid);
					}
				}
			}

			return ecP;
		}

		internal static ECPublicKeyParameters GetCorrespondingPublicKey(
			ECPrivateKeyParameters privKey)
		{
			ECDomainParameters parameters = privKey.Parameters;
			ECPoint q = parameters.G.Multiply(privKey.D);

			if (privKey.PublicKeyParamSet != null)
			{
				return new ECPublicKeyParameters(privKey.AlgorithmName, q, privKey.PublicKeyParamSet);
			}

			return new ECPublicKeyParameters(privKey.AlgorithmName, q, parameters);
		}
	}
}
