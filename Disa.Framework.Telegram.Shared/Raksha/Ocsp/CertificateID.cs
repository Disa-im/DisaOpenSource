using System;
using BigMath;
using Raksha.Asn1;
using Raksha.Asn1.Ocsp;
using Raksha.Asn1.X509;
using Raksha.Crypto;
using Raksha.Math;
using Raksha.Security;
using Raksha.X509;

namespace Raksha.Ocsp
{
	public class CertificateID
	{
		public const string HashSha1 = "1.3.14.3.2.26";

		private readonly CertID id;

		public CertificateID(
			CertID id)
		{
			if (id == null)
				throw new ArgumentNullException("id");

			this.id = id;
		}

		/**
		 * create from an issuer certificate and the serial number of the
		 * certificate it signed.
		 * @exception OcspException if any problems occur creating the id fields.
		 */
		public CertificateID(
			string			hashAlgorithm,
			X509Certificate	issuerCert,
			BigInteger		serialNumber)
		{
			AlgorithmIdentifier hashAlg = new AlgorithmIdentifier(
				new DerObjectIdentifier(hashAlgorithm), DerNull.Instance);

			this.id = createCertID(hashAlg, issuerCert, new DerInteger(serialNumber));
		}

		public string HashAlgOid
		{
			get { return id.HashAlgorithm.ObjectID.Id; }
		}

		public byte[] GetIssuerNameHash()
		{
			return id.IssuerNameHash.GetOctets();
		}

		public byte[] GetIssuerKeyHash()
		{
			return id.IssuerKeyHash.GetOctets();
		}

		/**
		 * return the serial number for the certificate associated
		 * with this request.
		 */
		public BigInteger SerialNumber
		{
			get { return id.SerialNumber.Value; }
		}

		public bool MatchesIssuer(
			X509Certificate	issuerCert)
		{
			return createCertID(id.HashAlgorithm, issuerCert, id.SerialNumber).Equals(id);
		}

		public CertID ToAsn1Object()
		{
			return id;
		}

		public override bool Equals(
			object obj)
		{
			if (obj == this)
				return true;

			CertificateID other = obj as CertificateID;

			if (other == null)
				return false;

			return id.ToAsn1Object().Equals(other.id.ToAsn1Object());
		}

		public override int GetHashCode()
		{
			return id.ToAsn1Object().GetHashCode();
		}

        private static CertID createCertID(
			AlgorithmIdentifier	hashAlg,
			X509Certificate		issuerCert,
			DerInteger			serialNumber)
		{
			try
			{
				String hashAlgorithm = hashAlg.ObjectID.Id;

				X509Name issuerName = PrincipalUtilities.GetSubjectX509Principal(issuerCert);
				byte[] issuerNameHash = DigestUtilities.CalculateDigest(
					hashAlgorithm, issuerName.GetEncoded());

				AsymmetricKeyParameter issuerKey = issuerCert.GetPublicKey();
				SubjectPublicKeyInfo info = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(issuerKey);
				byte[] issuerKeyHash = DigestUtilities.CalculateDigest(
					hashAlgorithm, info.PublicKeyData.GetBytes());

				return new CertID(hashAlg, new DerOctetString(issuerNameHash),
					new DerOctetString(issuerKeyHash), serialNumber);
			}
			catch (Exception e)
			{
				throw new OcspException("problem creating ID: " + e, e);
			}
		}
	}
}
