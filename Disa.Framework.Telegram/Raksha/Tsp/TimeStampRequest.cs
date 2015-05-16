using System;
using System.Collections;
using System.IO;
using BigMath;
using Raksha.Crypto;
using Raksha.Security;
using Raksha.Asn1;
using Raksha.Asn1.Cmp;
using Raksha.Asn1.Tsp;
using Raksha.Asn1.X509;
using Raksha.Math;
using Raksha.X509;

namespace Raksha.Tsp
{
	/**
	 * Base class for an RFC 3161 Time Stamp Request.
	 */
	public class TimeStampRequest
		: X509ExtensionBase
	{
		private TimeStampReq req;

		public TimeStampRequest(
			TimeStampReq req)
		{
			this.req = req;
		}

		/**
		* Create a TimeStampRequest from the past in byte array.
		*
		* @param req byte array containing the request.
		* @throws IOException if the request is malformed.
		*/
		public TimeStampRequest(
			byte[] req)
			: this(new Asn1InputStream(req))
		{
		}

		/**
		* Create a TimeStampRequest from the past in input stream.
		*
		* @param in input stream containing the request.
		* @throws IOException if the request is malformed.
		*/
		public TimeStampRequest(
			Stream input)
			: this(new Asn1InputStream(input))
		{
		}

		private TimeStampRequest(
			Asn1InputStream str)
		{
			try
			{
				this.req = TimeStampReq.GetInstance(str.ReadObject());
			}
			catch (InvalidCastException e)
			{
				throw new IOException("malformed request: " + e);
			}
			catch (ArgumentException e)
			{
				throw new IOException("malformed request: " + e);
			}
		}

		public int Version
		{
			get { return req.Version.Value.IntValue; }
		}

		public string MessageImprintAlgOid
		{
			get { return req.MessageImprint.HashAlgorithm.ObjectID.Id; }
		}

		public byte[] GetMessageImprintDigest()
		{
			return req.MessageImprint.GetHashedMessage();
		}

		public string ReqPolicy
		{
			get
			{
				return req.ReqPolicy == null
					?	null
					:	req.ReqPolicy.Id;
			}
		}

		public BigInteger Nonce
		{
			get
			{
				return req.Nonce == null
					?	null
					:	req.Nonce.Value;
			}
		}

		public bool CertReq
		{
			get
			{
				return req.CertReq == null
					?	false
					:	req.CertReq.IsTrue;
			}
		}

		/**
		* Validate the timestamp request, checking the digest to see if it is of an
		* accepted type and whether it is of the correct length for the algorithm specified.
		*
		* @param algorithms a set of string OIDS giving accepted algorithms.
		* @param policies if non-null a set of policies we are willing to sign under.
		* @param extensions if non-null a set of extensions we are willing to accept.
		* @throws TspException if the request is invalid, or processing fails.
		*/
		public void Validate(
			IList algorithms,
			IList policies,
			IList extensions)
		{
			if (!algorithms.Contains(this.MessageImprintAlgOid))
			{
				throw new TspValidationException("request contains unknown algorithm.", PkiFailureInfo.BadAlg);
			}

			if (policies != null && this.ReqPolicy != null && !policies.Contains(this.ReqPolicy))
			{
				throw new TspValidationException("request contains unknown policy.", PkiFailureInfo.UnacceptedPolicy);
			}

			if (this.Extensions != null && extensions != null)
			{
				foreach (DerObjectIdentifier oid in this.Extensions.ExtensionOids)
				{
					if (!extensions.Contains(oid.Id))
					{
						throw new TspValidationException("request contains unknown extension.",
							PkiFailureInfo.UnacceptedExtension);
					}
				}
			}

			int digestLength = TspUtil.GetDigestLength(this.MessageImprintAlgOid);

			if (digestLength != this.GetMessageImprintDigest().Length)
			{
				throw new TspValidationException("imprint digest the wrong length.",
					PkiFailureInfo.BadDataFormat);
			}
		}

		/**
		 * return the ASN.1 encoded representation of this object.
		 */
		public byte[] GetEncoded()
		{
			return req.GetEncoded();
		}

		internal X509Extensions Extensions
		{
			get { return req.Extensions; }
		}

		protected override X509Extensions GetX509Extensions()
		{
			return Extensions;
		}
	}
}
