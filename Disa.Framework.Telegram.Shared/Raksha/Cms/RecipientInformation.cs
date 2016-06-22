using System;
using System.IO;
using Raksha.Asn1.Nist;
using Raksha.Asn1.Pkcs;
using Raksha.Crypto.IO;
using Raksha.Utilities.Collections;
using Raksha.Asn1;
using Raksha.Asn1.X509;
using Raksha.Crypto;
using Raksha.Crypto.Parameters;
using Raksha.Security;
using Raksha.Utilities;

namespace Raksha.Cms
{
    public abstract class RecipientInformation
    {
		internal RecipientID			rid = new RecipientID();
		internal AlgorithmIdentifier	keyEncAlg;
		internal CmsSecureReadable		secureReadable;
		
		private byte[] resultMac;

		internal RecipientInformation(
			AlgorithmIdentifier	keyEncAlg,
			CmsSecureReadable	secureReadable)
		{
			this.keyEncAlg = keyEncAlg;
			this.secureReadable = secureReadable;
		}

		internal string GetContentAlgorithmName()
		{
			AlgorithmIdentifier algorithm = secureReadable.Algorithm;
//			return CmsEnvelopedHelper.Instance.GetSymmetricCipherName(algorithm.ObjectID.Id);
			return algorithm.ObjectID.Id;
		}

		public RecipientID RecipientID
        {
			get { return rid; }
        }

		public AlgorithmIdentifier KeyEncryptionAlgorithmID
		{
			get { return keyEncAlg; }
		}

		/**
        * return the object identifier for the key encryption algorithm.
        * 
		* @return OID for key encryption algorithm.
        */
        public string KeyEncryptionAlgOid
        {
			get { return keyEncAlg.ObjectID.Id; }
        }

		/**
        * return the ASN.1 encoded key encryption algorithm parameters, or null if
        * there aren't any.
        * 
		* @return ASN.1 encoding of key encryption algorithm parameters.
        */
		public Asn1Object KeyEncryptionAlgParams
		{
			get
			{
				Asn1Encodable ae = keyEncAlg.Parameters;

				return ae == null ? null : ae.ToAsn1Object();
			}
		}

		internal CmsTypedStream GetContentFromSessionKey(
			KeyParameter sKey)
		{
			ICmsReadable readable = secureReadable.GetReadable(sKey); 

			try
			{
				return new CmsTypedStream(readable.GetInputStream());
			}
			catch (IOException e)
			{
				throw new CmsException("error getting .", e);
			}
		}

		public byte[] GetContent(
            ICipherParameters key)
        {
            try
            {
				return CmsUtilities.StreamToByteArray(GetContentStream(key).ContentStream);
            }
            catch (IOException e)
            {
                throw new Exception("unable to parse internal stream: " + e);
            }
        }

		/**
		* Return the MAC calculated for the content stream. Note: this call is only meaningful once all
		* the content has been read.
		*
		* @return  byte array containing the mac.
		*/
		public byte[] GetMac()
		{
			if (resultMac == null)
			{
				object cryptoObject = secureReadable.CryptoObject;
				if (cryptoObject is IMac)
				{
					resultMac = MacUtilities.DoFinal((IMac)cryptoObject);
				}
			}

			return Arrays.Clone(resultMac);
		}
		
		public abstract CmsTypedStream GetContentStream(ICipherParameters key);
	}
}
