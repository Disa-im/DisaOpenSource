using System;
using BigMath;
using Raksha.Asn1;
using Raksha.Asn1.CryptoPro;
using Raksha.Asn1.Oiw;
using Raksha.Asn1.Pkcs;
using Raksha.Asn1.Sec;
using Raksha.Asn1.X509;
using Raksha.Asn1.X9;
using Raksha.Crypto;
using Raksha.Crypto.Parameters;
using Raksha.Math;
using Raksha.Security;
using Raksha.Utilities;

namespace Raksha.Pkcs
{
	public sealed class PrivateKeyInfoFactory
	{
		private PrivateKeyInfoFactory()
		{
		}

		public static PrivateKeyInfo CreatePrivateKeyInfo(
			AsymmetricKeyParameter key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (!key.IsPrivate)
				throw new ArgumentException("Public key passed - private key expected", "key");

			if (key is ElGamalPrivateKeyParameters)
			{
				ElGamalPrivateKeyParameters _key = (ElGamalPrivateKeyParameters)key;
				return new PrivateKeyInfo(
					new AlgorithmIdentifier(
					OiwObjectIdentifiers.ElGamalAlgorithm,
					new ElGamalParameter(
					_key.Parameters.P,
					_key.Parameters.G).ToAsn1Object()),
					new DerInteger(_key.X));
			}

			if (key is DsaPrivateKeyParameters)
			{
				DsaPrivateKeyParameters _key = (DsaPrivateKeyParameters)key;
				return new PrivateKeyInfo(
					new AlgorithmIdentifier(
					X9ObjectIdentifiers.IdDsa,
					new DsaParameter(
					_key.Parameters.P,
					_key.Parameters.Q,
					_key.Parameters.G).ToAsn1Object()),
					new DerInteger(_key.X));
			}

			if (key is DHPrivateKeyParameters)
			{
				DHPrivateKeyParameters _key = (DHPrivateKeyParameters)key;

				DHParameter p = new DHParameter(
					_key.Parameters.P, _key.Parameters.G, _key.Parameters.L);

				return new PrivateKeyInfo(
					new AlgorithmIdentifier(_key.AlgorithmOid, p.ToAsn1Object()),
					new DerInteger(_key.X));
			}

			if (key is RsaKeyParameters)
			{
				AlgorithmIdentifier algID = new AlgorithmIdentifier(
					PkcsObjectIdentifiers.RsaEncryption, DerNull.Instance);

				RsaPrivateKeyStructure keyStruct;
				if (key is RsaPrivateCrtKeyParameters)
				{
					RsaPrivateCrtKeyParameters _key = (RsaPrivateCrtKeyParameters)key;

					keyStruct = new RsaPrivateKeyStructure(
						_key.Modulus,
						_key.PublicExponent,
						_key.Exponent,
						_key.P,
						_key.Q,
						_key.DP,
						_key.DQ,
						_key.QInv);
				}
				else
				{
					RsaKeyParameters _key = (RsaKeyParameters) key;

					keyStruct = new RsaPrivateKeyStructure(
						_key.Modulus,
						BigInteger.Zero,
						_key.Exponent,
						BigInteger.Zero,
						BigInteger.Zero,
						BigInteger.Zero,
						BigInteger.Zero,
						BigInteger.Zero);
				}

				return new PrivateKeyInfo(algID, keyStruct.ToAsn1Object());
			}

			if (key is ECPrivateKeyParameters)
			{
				ECPrivateKeyParameters _key = (ECPrivateKeyParameters)key;
				AlgorithmIdentifier algID;
				ECPrivateKeyStructure ec;

				if (_key.AlgorithmName == "ECGOST3410")
				{
					if (_key.PublicKeyParamSet == null)
						throw Platform.CreateNotImplementedException("Not a CryptoPro parameter set");

					Gost3410PublicKeyAlgParameters gostParams = new Gost3410PublicKeyAlgParameters(
						_key.PublicKeyParamSet, CryptoProObjectIdentifiers.GostR3411x94CryptoProParamSet);

					algID = new AlgorithmIdentifier(
						CryptoProObjectIdentifiers.GostR3410x2001,
						gostParams.ToAsn1Object());

					// TODO Do we need to pass any parameters here?
					ec = new ECPrivateKeyStructure(_key.D);
				}
				else
				{
					X962Parameters x962;
					if (_key.PublicKeyParamSet == null)
					{
						ECDomainParameters kp = _key.Parameters;
						X9ECParameters ecP = new X9ECParameters(kp.Curve, kp.G, kp.N, kp.H, kp.GetSeed());

						x962 = new X962Parameters(ecP);
					}
					else
					{
						x962 = new X962Parameters(_key.PublicKeyParamSet);
					}

					Asn1Object x962Object = x962.ToAsn1Object();

					// TODO Possible to pass the publicKey bitstring here?
					ec = new ECPrivateKeyStructure(_key.D, x962Object);

					algID = new AlgorithmIdentifier(X9ObjectIdentifiers.IdECPublicKey, x962Object);
				}

				return new PrivateKeyInfo(algID, ec.ToAsn1Object());
			}

			if (key is Gost3410PrivateKeyParameters)
			{
				Gost3410PrivateKeyParameters _key = (Gost3410PrivateKeyParameters)key;

				if (_key.PublicKeyParamSet == null)
					throw Platform.CreateNotImplementedException("Not a CryptoPro parameter set");

				byte[] keyEnc = _key.X.ToByteArrayUnsigned();
				byte[] keyBytes = new byte[keyEnc.Length];

				for (int i = 0; i != keyBytes.Length; i++)
				{
					keyBytes[i] = keyEnc[keyEnc.Length - 1 - i]; // must be little endian
				}

				Gost3410PublicKeyAlgParameters algParams = new Gost3410PublicKeyAlgParameters(
					_key.PublicKeyParamSet, CryptoProObjectIdentifiers.GostR3411x94CryptoProParamSet, null);

				AlgorithmIdentifier algID = new AlgorithmIdentifier(
					CryptoProObjectIdentifiers.GostR3410x94,
					algParams.ToAsn1Object());

				return new PrivateKeyInfo(algID, new DerOctetString(keyBytes));
			}

			throw new ArgumentException("Class provided is not convertible: " + key.GetType().FullName);
		}

		public static PrivateKeyInfo CreatePrivateKeyInfo(
			char[]					passPhrase,
			EncryptedPrivateKeyInfo	encInfo)
		{
			return CreatePrivateKeyInfo(passPhrase, false, encInfo);
		}

		public static PrivateKeyInfo CreatePrivateKeyInfo(
			char[]					passPhrase,
			bool					wrongPkcs12Zero,
			EncryptedPrivateKeyInfo	encInfo)
		{
			AlgorithmIdentifier algID = encInfo.EncryptionAlgorithm;
			IBufferedCipher cipher = PbeUtilities.CreateEngine(algID) as IBufferedCipher;

			if (cipher == null)
			{
				// TODO Throw exception?
			}

			ICipherParameters keyParameters = PbeUtilities.GenerateCipherParameters(
				algID, passPhrase, wrongPkcs12Zero);

			cipher.Init(false, keyParameters);

			byte[] keyBytes = encInfo.GetEncryptedData();
			byte[] encoding = cipher.DoFinal(keyBytes);
			Asn1Object asn1Data = Asn1Object.FromByteArray(encoding);

			return PrivateKeyInfo.GetInstance(asn1Data);
		}
	}
}
