using System;
using System.Globalization;
using Raksha.Asn1;
using Raksha.Asn1.CryptoPro;
using Raksha.Asn1.X9;
using Raksha.Crypto.Generators;
using Raksha.Security;

namespace Raksha.Crypto.Parameters
{
    public abstract class ECKeyParameters
		: AsymmetricKeyParameter
    {
		private readonly string algorithm;
		private readonly ECDomainParameters parameters;
		private readonly DerObjectIdentifier publicKeyParamSet;

		protected ECKeyParameters(
			string				algorithm,
            bool				isPrivate,
            ECDomainParameters	parameters)
			: base(isPrivate)
        {
			if (algorithm == null)
				throw new ArgumentNullException("algorithm");
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			this.algorithm = VerifyAlgorithmName(algorithm);
			this.parameters = parameters;
        }

		protected ECKeyParameters(
			string				algorithm,
			bool				isPrivate,
			DerObjectIdentifier	publicKeyParamSet)
			: base(isPrivate)
		{
			if (algorithm == null)
				throw new ArgumentNullException("algorithm");
			if (publicKeyParamSet == null)
				throw new ArgumentNullException("publicKeyParamSet");

			this.algorithm = VerifyAlgorithmName(algorithm);
			this.parameters = LookupParameters(publicKeyParamSet);
			this.publicKeyParamSet = publicKeyParamSet;
		}

		public string AlgorithmName
		{
			get { return algorithm; }
		}

		public ECDomainParameters Parameters
        {
			get { return parameters; }
        }

		public DerObjectIdentifier PublicKeyParamSet
		{
			get { return publicKeyParamSet; }
		}

		public override bool Equals(
			object obj)
		{
			if (obj == this)
				return true;

			ECDomainParameters other = obj as ECDomainParameters;

			if (other == null)
				return false;

			return Equals(other);
		}

		protected bool Equals(
			ECKeyParameters other)
		{
			return parameters.Equals(other.parameters) && base.Equals(other);
		}

		public override int GetHashCode()
		{
			return parameters.GetHashCode() ^ base.GetHashCode();
		}

		internal ECKeyGenerationParameters CreateKeyGenerationParameters(
			SecureRandom random)
		{
			if (publicKeyParamSet != null)
			{
				return new ECKeyGenerationParameters(publicKeyParamSet, random);
			}

			return new ECKeyGenerationParameters(parameters, random);
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

		internal static ECDomainParameters LookupParameters(
			DerObjectIdentifier publicKeyParamSet)
		{
			if (publicKeyParamSet == null)
				throw new ArgumentNullException("publicKeyParamSet");

			ECDomainParameters p = ECGost3410NamedCurves.GetByOid(publicKeyParamSet);

			if (p == null)
			{
				X9ECParameters x9 = ECKeyPairGenerator.FindECCurveByOid(publicKeyParamSet);

				if (x9 == null)
				{
					throw new ArgumentException("OID is not a valid public key parameter set", "publicKeyParamSet");
				}

				p = new ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H, x9.GetSeed());
			}

			return p;
		}
	}
}
