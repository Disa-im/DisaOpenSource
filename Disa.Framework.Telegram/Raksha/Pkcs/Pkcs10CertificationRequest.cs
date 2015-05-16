using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Raksha.Asn1;
using Raksha.Asn1.CryptoPro;
using Raksha.Asn1.Nist;
using Raksha.Asn1.Oiw;
using Raksha.Asn1.Pkcs;
using Raksha.Asn1.TeleTrust;
using Raksha.Asn1.X509;
using Raksha.Asn1.X9;
using Raksha.Crypto;
using Raksha.Security;
using Raksha.Utilities;
using Raksha.Utilities.Collections;
using Raksha.X509;

namespace Raksha.Pkcs
{
	/// <remarks>
	/// A class for verifying and creating Pkcs10 Certification requests.
	/// </remarks>
	/// <code>
	/// CertificationRequest ::= Sequence {
	///   certificationRequestInfo  CertificationRequestInfo,
	///   signatureAlgorithm        AlgorithmIdentifier{{ SignatureAlgorithms }},
	///   signature                 BIT STRING
	/// }
	///
	/// CertificationRequestInfo ::= Sequence {
	///   version             Integer { v1(0) } (v1,...),
	///   subject             Name,
	///   subjectPKInfo   SubjectPublicKeyInfo{{ PKInfoAlgorithms }},
	///   attributes          [0] Attributes{{ CRIAttributes }}
	///  }
	///
	///  Attributes { ATTRIBUTE:IOSet } ::= Set OF Attr{{ IOSet }}
	///
	///  Attr { ATTRIBUTE:IOSet } ::= Sequence {
	///    type    ATTRIBUTE.&amp;id({IOSet}),
	///    values  Set SIZE(1..MAX) OF ATTRIBUTE.&amp;Type({IOSet}{\@type})
	///  }
	/// </code>
	/// see <a href="http://www.rsasecurity.com/rsalabs/node.asp?id=2132"/>
	public class Pkcs10CertificationRequest
		: CertificationRequest
	{
		protected static readonly IDictionary algorithms = Platform.CreateHashtable();
        protected static readonly IDictionary exParams = Platform.CreateHashtable();
        protected static readonly IDictionary keyAlgorithms = Platform.CreateHashtable();
        protected static readonly IDictionary oids = Platform.CreateHashtable();
		protected static readonly ISet noParams = new HashSet();

		static Pkcs10CertificationRequest()
		{
			algorithms.Add("MD2WITHRSAENCRYPTION", new DerObjectIdentifier("1.2.840.113549.1.1.2"));
			algorithms.Add("MD2WITHRSA", new DerObjectIdentifier("1.2.840.113549.1.1.2"));
			algorithms.Add("MD5WITHRSAENCRYPTION", new DerObjectIdentifier("1.2.840.113549.1.1.4"));
			algorithms.Add("MD5WITHRSA", new DerObjectIdentifier("1.2.840.113549.1.1.4"));
			algorithms.Add("RSAWITHMD5", new DerObjectIdentifier("1.2.840.113549.1.1.4"));
			algorithms.Add("SHA1WITHRSAENCRYPTION", new DerObjectIdentifier("1.2.840.113549.1.1.5"));
			algorithms.Add("SHA1WITHRSA", new DerObjectIdentifier("1.2.840.113549.1.1.5"));
			algorithms.Add("SHA224WITHRSAENCRYPTION", PkcsObjectIdentifiers.Sha224WithRsaEncryption);
			algorithms.Add("SHA224WITHRSA", PkcsObjectIdentifiers.Sha224WithRsaEncryption);
			algorithms.Add("SHA256WITHRSAENCRYPTION", PkcsObjectIdentifiers.Sha256WithRsaEncryption);
			algorithms.Add("SHA256WITHRSA", PkcsObjectIdentifiers.Sha256WithRsaEncryption);
			algorithms.Add("SHA384WITHRSAENCRYPTION", PkcsObjectIdentifiers.Sha384WithRsaEncryption);
			algorithms.Add("SHA384WITHRSA", PkcsObjectIdentifiers.Sha384WithRsaEncryption);
			algorithms.Add("SHA512WITHRSAENCRYPTION", PkcsObjectIdentifiers.Sha512WithRsaEncryption);
			algorithms.Add("SHA512WITHRSA", PkcsObjectIdentifiers.Sha512WithRsaEncryption);
			algorithms.Add("SHA1WITHRSAANDMGF1", PkcsObjectIdentifiers.IdRsassaPss);
			algorithms.Add("SHA224WITHRSAANDMGF1", PkcsObjectIdentifiers.IdRsassaPss);
			algorithms.Add("SHA256WITHRSAANDMGF1", PkcsObjectIdentifiers.IdRsassaPss);
			algorithms.Add("SHA384WITHRSAANDMGF1", PkcsObjectIdentifiers.IdRsassaPss);
			algorithms.Add("SHA512WITHRSAANDMGF1", PkcsObjectIdentifiers.IdRsassaPss);
			algorithms.Add("RSAWITHSHA1", new DerObjectIdentifier("1.2.840.113549.1.1.5"));
			algorithms.Add("RIPEMD160WITHRSAENCRYPTION", new DerObjectIdentifier("1.3.36.3.3.1.2"));
			algorithms.Add("RIPEMD160WITHRSA", new DerObjectIdentifier("1.3.36.3.3.1.2"));
			algorithms.Add("SHA1WITHDSA", new DerObjectIdentifier("1.2.840.10040.4.3"));
			algorithms.Add("DSAWITHSHA1", new DerObjectIdentifier("1.2.840.10040.4.3"));
			algorithms.Add("SHA224WITHDSA", NistObjectIdentifiers.DsaWithSha224);
			algorithms.Add("SHA256WITHDSA", NistObjectIdentifiers.DsaWithSha256);
			algorithms.Add("SHA1WITHECDSA", X9ObjectIdentifiers.ECDsaWithSha1);
			algorithms.Add("SHA224WITHECDSA", X9ObjectIdentifiers.ECDsaWithSha224);
			algorithms.Add("SHA256WITHECDSA", X9ObjectIdentifiers.ECDsaWithSha256);
			algorithms.Add("SHA384WITHECDSA", X9ObjectIdentifiers.ECDsaWithSha384);
			algorithms.Add("SHA512WITHECDSA", X9ObjectIdentifiers.ECDsaWithSha512);
			algorithms.Add("ECDSAWITHSHA1", X9ObjectIdentifiers.ECDsaWithSha1);
			algorithms.Add("GOST3411WITHGOST3410", CryptoProObjectIdentifiers.GostR3411x94WithGostR3410x94);
			algorithms.Add("GOST3410WITHGOST3411", CryptoProObjectIdentifiers.GostR3411x94WithGostR3410x94);
			algorithms.Add("GOST3411WITHECGOST3410", CryptoProObjectIdentifiers.GostR3411x94WithGostR3410x2001);
			algorithms.Add("GOST3411WITHECGOST3410-2001", CryptoProObjectIdentifiers.GostR3411x94WithGostR3410x2001);
			algorithms.Add("GOST3411WITHGOST3410-2001", CryptoProObjectIdentifiers.GostR3411x94WithGostR3410x2001);

			//
			// reverse mappings
			//
			oids.Add(new DerObjectIdentifier("1.2.840.113549.1.1.5"), "SHA1WITHRSA");
			oids.Add(PkcsObjectIdentifiers.Sha224WithRsaEncryption, "SHA224WITHRSA");
			oids.Add(PkcsObjectIdentifiers.Sha256WithRsaEncryption, "SHA256WITHRSA");
			oids.Add(PkcsObjectIdentifiers.Sha384WithRsaEncryption, "SHA384WITHRSA");
			oids.Add(PkcsObjectIdentifiers.Sha512WithRsaEncryption, "SHA512WITHRSA");
			oids.Add(CryptoProObjectIdentifiers.GostR3411x94WithGostR3410x94, "GOST3411WITHGOST3410");
			oids.Add(CryptoProObjectIdentifiers.GostR3411x94WithGostR3410x2001, "GOST3411WITHECGOST3410");

			oids.Add(new DerObjectIdentifier("1.2.840.113549.1.1.4"), "MD5WITHRSA");
			oids.Add(new DerObjectIdentifier("1.2.840.113549.1.1.2"), "MD2WITHRSA");
			oids.Add(new DerObjectIdentifier("1.2.840.10040.4.3"), "SHA1WITHDSA");
			oids.Add(X9ObjectIdentifiers.ECDsaWithSha1, "SHA1WITHECDSA");
			oids.Add(X9ObjectIdentifiers.ECDsaWithSha224, "SHA224WITHECDSA");
			oids.Add(X9ObjectIdentifiers.ECDsaWithSha256, "SHA256WITHECDSA");
			oids.Add(X9ObjectIdentifiers.ECDsaWithSha384, "SHA384WITHECDSA");
			oids.Add(X9ObjectIdentifiers.ECDsaWithSha512, "SHA512WITHECDSA");
			oids.Add(OiwObjectIdentifiers.Sha1WithRsa, "SHA1WITHRSA");
			oids.Add(OiwObjectIdentifiers.DsaWithSha1, "SHA1WITHDSA");
			oids.Add(NistObjectIdentifiers.DsaWithSha224, "SHA224WITHDSA");
			oids.Add(NistObjectIdentifiers.DsaWithSha256, "SHA256WITHDSA");

			//
			// key types
			//
			keyAlgorithms.Add(PkcsObjectIdentifiers.RsaEncryption, "RSA");
			keyAlgorithms.Add(X9ObjectIdentifiers.IdDsa, "DSA");

			//
			// According to RFC 3279, the ASN.1 encoding SHALL (id-dsa-with-sha1) or MUST (ecdsa-with-SHA*) omit the parameters field.
			// The parameters field SHALL be NULL for RSA based signature algorithms.
			//
			noParams.Add(X9ObjectIdentifiers.ECDsaWithSha1);
			noParams.Add(X9ObjectIdentifiers.ECDsaWithSha224);
			noParams.Add(X9ObjectIdentifiers.ECDsaWithSha256);
			noParams.Add(X9ObjectIdentifiers.ECDsaWithSha384);
			noParams.Add(X9ObjectIdentifiers.ECDsaWithSha512);
			noParams.Add(X9ObjectIdentifiers.IdDsaWithSha1);
			noParams.Add(NistObjectIdentifiers.DsaWithSha224);
			noParams.Add(NistObjectIdentifiers.DsaWithSha256);

			//
			// RFC 4491
			//
			noParams.Add(CryptoProObjectIdentifiers.GostR3411x94WithGostR3410x94);
			noParams.Add(CryptoProObjectIdentifiers.GostR3411x94WithGostR3410x2001);

			//
			// explicit params
			//
			AlgorithmIdentifier sha1AlgId = new AlgorithmIdentifier(OiwObjectIdentifiers.IdSha1, DerNull.Instance);
			exParams.Add("SHA1WITHRSAANDMGF1", CreatePssParams(sha1AlgId, 20));

			AlgorithmIdentifier sha224AlgId = new AlgorithmIdentifier(NistObjectIdentifiers.IdSha224, DerNull.Instance);
			exParams.Add("SHA224WITHRSAANDMGF1", CreatePssParams(sha224AlgId, 28));

			AlgorithmIdentifier sha256AlgId = new AlgorithmIdentifier(NistObjectIdentifiers.IdSha256, DerNull.Instance);
			exParams.Add("SHA256WITHRSAANDMGF1", CreatePssParams(sha256AlgId, 32));

			AlgorithmIdentifier sha384AlgId = new AlgorithmIdentifier(NistObjectIdentifiers.IdSha384, DerNull.Instance);
			exParams.Add("SHA384WITHRSAANDMGF1", CreatePssParams(sha384AlgId, 48));

			AlgorithmIdentifier sha512AlgId = new AlgorithmIdentifier(NistObjectIdentifiers.IdSha512, DerNull.Instance);
			exParams.Add("SHA512WITHRSAANDMGF1", CreatePssParams(sha512AlgId, 64));
		}

		private static RsassaPssParameters CreatePssParams(
			AlgorithmIdentifier	hashAlgId,
			int					saltSize)
		{
			return new RsassaPssParameters(
				hashAlgId,
				new AlgorithmIdentifier(PkcsObjectIdentifiers.IdMgf1, hashAlgId),
				new DerInteger(saltSize),
				new DerInteger(1));
		}

		protected Pkcs10CertificationRequest()
		{
		}

		public Pkcs10CertificationRequest(
			byte[] encoded)
			: base((Asn1Sequence) Asn1Object.FromByteArray(encoded))
		{
		}

		public Pkcs10CertificationRequest(
			Asn1Sequence seq)
			: base(seq)
		{
		}

		public Pkcs10CertificationRequest(
			Stream input)
			: base((Asn1Sequence) Asn1Object.FromStream(input))
		{
		}

		/// <summary>
		/// Instantiate a Pkcs10CertificationRequest object with the necessary credentials.
		/// </summary>
		///<param name="signatureAlgorithm">Name of Sig Alg.</param>
		/// <param name="subject">X509Name of subject eg OU="My unit." O="My Organisatioin" C="au" </param>
		/// <param name="publicKey">Public Key to be included in cert reqest.</param>
		/// <param name="attributes">ASN1Set of Attributes.</param>
		/// <param name="signingKey">Matching Private key for nominated (above) public key to be used to sign the request.</param>
		public Pkcs10CertificationRequest(
			string					signatureAlgorithm,
			X509Name				subject,
			AsymmetricKeyParameter	publicKey,
			Asn1Set					attributes,
			AsymmetricKeyParameter	signingKey)
		{
			if (signatureAlgorithm == null)
				throw new ArgumentNullException("signatureAlgorithm");
			if (subject == null)
				throw new ArgumentNullException("subject");
			if (publicKey == null)
				throw new ArgumentNullException("publicKey");
			if (publicKey.IsPrivate)
				throw new ArgumentException("expected public key", "publicKey");
			if (!signingKey.IsPrivate)
				throw new ArgumentException("key for signing must be private", "signingKey");

//			DerObjectIdentifier sigOid = SignerUtilities.GetObjectIdentifier(signatureAlgorithm);
			string algorithmName = signatureAlgorithm.ToUpperInvariant();
			DerObjectIdentifier sigOid = (DerObjectIdentifier) algorithms[algorithmName];

			if (sigOid == null)
			{
				try
				{
					sigOid = new DerObjectIdentifier(algorithmName);
				}
				catch (Exception e)
				{
					throw new ArgumentException("Unknown signature type requested", e);
				}
			}

			if (noParams.Contains(sigOid))
			{
				this.sigAlgId = new AlgorithmIdentifier(sigOid);
			}
			else if (exParams.Contains(algorithmName))
			{
				this.sigAlgId = new AlgorithmIdentifier(sigOid, (Asn1Encodable) exParams[algorithmName]);
			}
			else
			{
				this.sigAlgId = new AlgorithmIdentifier(sigOid, DerNull.Instance);
			}

			SubjectPublicKeyInfo pubInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);

			this.reqInfo = new CertificationRequestInfo(subject, pubInfo, attributes);

			ISigner sig = SignerUtilities.GetSigner(signatureAlgorithm);

			sig.Init(true, signingKey);

			try
			{
				// Encode.
				byte[] b = reqInfo.GetDerEncoded();
				sig.BlockUpdate(b, 0, b.Length);
			}
			catch (Exception e)
			{
				throw new ArgumentException("exception encoding TBS cert request", e);
			}

			// Generate Signature.
			sigBits = new DerBitString(sig.GenerateSignature());
		}

//        internal Pkcs10CertificationRequest(
//        	Asn1InputStream seqStream)
//        {
//			Asn1Sequence seq = (Asn1Sequence) seqStream.ReadObject();
//            try
//            {
//                this.reqInfo = CertificationRequestInfo.GetInstance(seq[0]);
//                this.sigAlgId = AlgorithmIdentifier.GetInstance(seq[1]);
//                this.sigBits = (DerBitString) seq[2];
//            }
//            catch (Exception ex)
//            {
//                throw new ArgumentException("Create From Asn1Sequence: " + ex.Message);
//            }
//        }

		/// <summary>
		/// Get the public key.
		/// </summary>
		/// <returns>The public key.</returns>
		public AsymmetricKeyParameter GetPublicKey()
		{
			return PublicKeyFactory.CreateKey(reqInfo.SubjectPublicKeyInfo);
		}

		/// <summary>
		/// Verify Pkcs10 Cert Request is valid.
		/// </summary>
		/// <returns>true = valid.</returns>
		public bool Verify()
		{
			return Verify(this.GetPublicKey());
		}

		public bool Verify(
			AsymmetricKeyParameter publicKey)
		{
			ISigner sig;

			try
			{
				sig = SignerUtilities.GetSigner(GetSignatureName(sigAlgId));
			}
			catch (Exception e)
			{
				// try an alternate
				string alt = (string) oids[sigAlgId.ObjectID];

				if (alt != null)
				{
					sig = SignerUtilities.GetSigner(alt);
				}
				else
				{
					throw e;
				}
			}

			SetSignatureParameters(sig, sigAlgId.Parameters);

			sig.Init(false, publicKey);

			try
			{
				byte[] b = reqInfo.GetDerEncoded();
				sig.BlockUpdate(b, 0, b.Length);
			}
			catch (Exception e)
			{
				throw new SignatureException("exception encoding TBS cert request", e);
			}

			return sig.VerifySignature(sigBits.GetBytes());
		}

//        /// <summary>
//        /// Get the Der Encoded Pkcs10 Certification Request.
//        /// </summary>
//        /// <returns>A byte array.</returns>
//        public byte[] GetEncoded()
//        {
//        	return new CertificationRequest(reqInfo, sigAlgId, sigBits).GetDerEncoded();
//        }

		// TODO Figure out how to set parameters on an ISigner
		private void SetSignatureParameters(
			ISigner			signature,
			Asn1Encodable	asn1Params)
		{
			if (asn1Params != null && !(asn1Params is Asn1Null))
			{
//				AlgorithmParameters sigParams = AlgorithmParameters.GetInstance(signature.getAlgorithm());
//
//				try
//				{
//					sigParams.init(asn1Params.ToAsn1Object().GetDerEncoded());
//				}
//				catch (IOException e)
//				{
//					throw new SignatureException("IOException decoding parameters: " + e.Message);
//				}

				if (signature.AlgorithmName.EndsWith("MGF1"))
				{
					throw Platform.CreateNotImplementedException("signature algorithm with MGF1");

//					try
//					{
//						signature.setParameter(sigParams.getParameterSpec(PSSParameterSpec.class));
//					}
//					catch (GeneralSecurityException e)
//					{
//						throw new SignatureException("Exception extracting parameters: " + e.getMessage());
//					}
				}
			}
		}

		internal static string GetSignatureName(
			AlgorithmIdentifier sigAlgId)
		{
			Asn1Encodable asn1Params = sigAlgId.Parameters;

			if (asn1Params != null && !(asn1Params is Asn1Null))
			{
				if (sigAlgId.ObjectID.Equals(PkcsObjectIdentifiers.IdRsassaPss))
				{
					RsassaPssParameters rsaParams = RsassaPssParameters.GetInstance(asn1Params);
					return GetDigestAlgName(rsaParams.HashAlgorithm.ObjectID) + "withRSAandMGF1";
				}
			}

			return sigAlgId.ObjectID.Id;
		}

		private static string GetDigestAlgName(
			DerObjectIdentifier digestAlgOID)
		{
			if (PkcsObjectIdentifiers.MD5.Equals(digestAlgOID))
			{
				return "MD5";
			}
			else if (OiwObjectIdentifiers.IdSha1.Equals(digestAlgOID))
			{
				return "SHA1";
			}
			else if (NistObjectIdentifiers.IdSha224.Equals(digestAlgOID))
			{
				return "SHA224";
			}
			else if (NistObjectIdentifiers.IdSha256.Equals(digestAlgOID))
			{
				return "SHA256";
			}
			else if (NistObjectIdentifiers.IdSha384.Equals(digestAlgOID))
			{
				return "SHA384";
			}
			else if (NistObjectIdentifiers.IdSha512.Equals(digestAlgOID))
			{
				return "SHA512";
			}
			else if (TeleTrusTObjectIdentifiers.RipeMD128.Equals(digestAlgOID))
			{
				return "RIPEMD128";
			}
			else if (TeleTrusTObjectIdentifiers.RipeMD160.Equals(digestAlgOID))
			{
				return "RIPEMD160";
			}
			else if (TeleTrusTObjectIdentifiers.RipeMD256.Equals(digestAlgOID))
			{
				return "RIPEMD256";
			}
			else if (CryptoProObjectIdentifiers.GostR3411.Equals(digestAlgOID))
			{
				return "GOST3411";
			}
			else
			{
				return digestAlgOID.Id;
			}
		}
	}
}
