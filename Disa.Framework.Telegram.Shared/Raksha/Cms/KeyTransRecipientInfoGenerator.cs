using System;
using System.IO;
using Raksha.Asn1;
using Raksha.Asn1.Cms;
using Raksha.Asn1.X509;
using Raksha.Crypto;
using Raksha.Crypto.Parameters;
using Raksha.Security;
using Raksha.X509;

namespace Raksha.Cms
{
	internal class KeyTransRecipientInfoGenerator : RecipientInfoGenerator
	{
		private static readonly CmsEnvelopedHelper Helper = CmsEnvelopedHelper.Instance;

		private TbsCertificateStructure	recipientTbsCert;
		private AsymmetricKeyParameter	recipientPublicKey;
		private Asn1OctetString			subjectKeyIdentifier;

		// Derived fields
		private SubjectPublicKeyInfo info;

		internal KeyTransRecipientInfoGenerator()
		{
		}

		internal X509Certificate RecipientCert
		{
			set
			{
				this.recipientTbsCert = CmsUtilities.GetTbsCertificateStructure(value);
				this.recipientPublicKey = value.GetPublicKey();
				this.info = recipientTbsCert.SubjectPublicKeyInfo;
			}
		}
		
		internal AsymmetricKeyParameter RecipientPublicKey
		{
			set
			{
				this.recipientPublicKey = value;

				try
				{
					info = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(
						recipientPublicKey);
				}
				catch (IOException)
				{
					throw new ArgumentException("can't extract key algorithm from this key");
				}
			}
		}
		
		internal Asn1OctetString SubjectKeyIdentifier
		{
			set { this.subjectKeyIdentifier = value; }
		}

		public RecipientInfo Generate(KeyParameter contentEncryptionKey, SecureRandom random)
		{
			byte[] keyBytes = contentEncryptionKey.GetKey();
			AlgorithmIdentifier keyEncryptionAlgorithm = info.AlgorithmID;

			IWrapper keyWrapper = Helper.CreateWrapper(keyEncryptionAlgorithm.ObjectID.Id);
			keyWrapper.Init(true, new ParametersWithRandom(recipientPublicKey, random));
			byte[] encryptedKeyBytes = keyWrapper.Wrap(keyBytes, 0, keyBytes.Length);

			RecipientIdentifier recipId;
			if (recipientTbsCert != null)
			{
				IssuerAndSerialNumber issuerAndSerial = new IssuerAndSerialNumber(
					recipientTbsCert.Issuer, recipientTbsCert.SerialNumber.Value);
				recipId = new RecipientIdentifier(issuerAndSerial);
			}
			else
			{
				recipId = new RecipientIdentifier(subjectKeyIdentifier);
			}

			return new RecipientInfo(new KeyTransRecipientInfo(recipId, keyEncryptionAlgorithm,
				new DerOctetString(encryptedKeyBytes)));
		}
	}
}
