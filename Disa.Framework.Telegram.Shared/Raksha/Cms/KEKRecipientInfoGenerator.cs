using System;
using Raksha.Asn1;
using Raksha.Asn1.Cms;
using Raksha.Asn1.Kisa;
using Raksha.Asn1.Nist;
using Raksha.Asn1.Ntt;
using Raksha.Asn1.Pkcs;
using Raksha.Asn1.X509;
using Raksha.Crypto;
using Raksha.Crypto.Parameters;
using Raksha.Security;

namespace Raksha.Cms
{
	internal class KekRecipientInfoGenerator : RecipientInfoGenerator
	{
		private static readonly CmsEnvelopedHelper Helper = CmsEnvelopedHelper.Instance;

		private KeyParameter	keyEncryptionKey;
		// TODO Can get this from keyEncryptionKey?		
		private string			keyEncryptionKeyOID;
		private KekIdentifier	kekIdentifier;

		// Derived
		private AlgorithmIdentifier keyEncryptionAlgorithm;

		internal KekRecipientInfoGenerator()
		{
		}

		internal KekIdentifier KekIdentifier
		{
			set { this.kekIdentifier = value; }
		}

		internal KeyParameter KeyEncryptionKey
		{
			set
			{
				this.keyEncryptionKey = value;
				this.keyEncryptionAlgorithm = DetermineKeyEncAlg(keyEncryptionKeyOID, keyEncryptionKey);
			}
		}

		internal string KeyEncryptionKeyOID
		{
			set { this.keyEncryptionKeyOID = value; }
		}

		public RecipientInfo Generate(KeyParameter contentEncryptionKey, SecureRandom random)
		{
			byte[] keyBytes = contentEncryptionKey.GetKey();

			IWrapper keyWrapper = Helper.CreateWrapper(keyEncryptionAlgorithm.ObjectID.Id);
			keyWrapper.Init(true, new ParametersWithRandom(keyEncryptionKey, random));
        	Asn1OctetString encryptedKey = new DerOctetString(
				keyWrapper.Wrap(keyBytes, 0, keyBytes.Length));

			return new RecipientInfo(new KekRecipientInfo(kekIdentifier, keyEncryptionAlgorithm, encryptedKey));
		}

		private static AlgorithmIdentifier DetermineKeyEncAlg(
			string algorithm, KeyParameter key)
		{
			if (algorithm.StartsWith("DES"))
			{
				return new AlgorithmIdentifier(
					PkcsObjectIdentifiers.IdAlgCms3DesWrap,
					DerNull.Instance);
			}
			else if (algorithm.StartsWith("RC2"))
			{
				return new AlgorithmIdentifier(
					PkcsObjectIdentifiers.IdAlgCmsRC2Wrap,
					new DerInteger(58));
			}
			else if (algorithm.StartsWith("AES"))
			{
				int length = key.GetKey().Length * 8;
				DerObjectIdentifier wrapOid;

				if (length == 128)
				{
					wrapOid = NistObjectIdentifiers.IdAes128Wrap;
				}
				else if (length == 192)
				{
					wrapOid = NistObjectIdentifiers.IdAes192Wrap;
				}
				else if (length == 256)
				{
					wrapOid = NistObjectIdentifiers.IdAes256Wrap;
				}
				else
				{
					throw new ArgumentException("illegal keysize in AES");
				}

				return new AlgorithmIdentifier(wrapOid);  // parameters absent
			}
			else if (algorithm.StartsWith("SEED"))
			{
				// parameters absent
				return new AlgorithmIdentifier(KisaObjectIdentifiers.IdNpkiAppCmsSeedWrap);
			}
			else if (algorithm.StartsWith("CAMELLIA"))
			{
				int length = key.GetKey().Length * 8;
				DerObjectIdentifier wrapOid;

				if (length == 128)
				{
					wrapOid = NttObjectIdentifiers.IdCamellia128Wrap;
				}
				else if (length == 192)
				{
					wrapOid = NttObjectIdentifiers.IdCamellia192Wrap;
				}
				else if (length == 256)
				{
					wrapOid = NttObjectIdentifiers.IdCamellia256Wrap;
				}
				else
				{
					throw new ArgumentException("illegal keysize in Camellia");
				}

				return new AlgorithmIdentifier(wrapOid); // parameters must be absent
			}
			else
			{
				throw new ArgumentException("unknown algorithm");
			}
		}
	}
}
