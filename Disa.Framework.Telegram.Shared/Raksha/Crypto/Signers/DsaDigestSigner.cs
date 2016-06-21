using System;
using System.Collections;
using System.IO;
using System.Text;
using BigMath;
using Raksha.Crypto.Signers;
using Raksha.Asn1;
using Raksha.Crypto.Parameters;
using Raksha.Math;
using Raksha.Security;

namespace Raksha.Crypto.Signers
{
	public class DsaDigestSigner
		: ISigner
	{
		private readonly IDigest digest;
		private readonly IDsa dsaSigner;
		private bool forSigning;

		public DsaDigestSigner(
			IDsa	signer,
			IDigest	digest)
		{
			this.digest = digest;
			this.dsaSigner = signer;
		}

		public string AlgorithmName
		{
			get { return digest.AlgorithmName + "with" + dsaSigner.AlgorithmName; }
		}

		public void Init(
			bool							forSigning,
			ICipherParameters	parameters)
		{
			this.forSigning = forSigning;

			AsymmetricKeyParameter k;

			if (parameters is ParametersWithRandom)
			{
				k = (AsymmetricKeyParameter)((ParametersWithRandom)parameters).Parameters;
			}
			else
			{
				k = (AsymmetricKeyParameter)parameters;
			}

			if (forSigning && !k.IsPrivate)
				throw new InvalidKeyException("Signing Requires Private Key.");

			if (!forSigning && k.IsPrivate)
				throw new InvalidKeyException("Verification Requires Public Key.");

			Reset();

			dsaSigner.Init(forSigning, parameters);
		}

		/**
		 * update the internal digest with the byte b
		 */
		public void Update(
			byte input)
		{
			digest.Update(input);
		}

		/**
		 * update the internal digest with the byte array in
		 */
		public void BlockUpdate(
			byte[]	input,
			int			inOff,
			int			length)
		{
			digest.BlockUpdate(input, inOff, length);
		}

		/**
		 * Generate a signature for the message we've been loaded with using
		 * the key we were initialised with.
     */
		public byte[] GenerateSignature()
		{
			if (!forSigning)
				throw new InvalidOperationException("DSADigestSigner not initialised for signature generation.");

			byte[] hash = new byte[digest.GetDigestSize()];
			digest.DoFinal(hash, 0);

			BigInteger[] sig = dsaSigner.GenerateSignature(hash);

			return DerEncode(sig[0], sig[1]);
		}

		/// <returns>true if the internal state represents the signature described in the passed in array.</returns>
		public bool VerifySignature(
			byte[] signature)
		{
			if (forSigning)
				throw new InvalidOperationException("DSADigestSigner not initialised for verification");

			byte[] hash = new byte[digest.GetDigestSize()];
			digest.DoFinal(hash, 0);

			try
			{
				BigInteger[] sig = DerDecode(signature);
				return dsaSigner.VerifySignature(hash, sig[0], sig[1]);
			}
			catch (IOException)
			{
				return false;
			}
		}

		/// <summary>Reset the internal state</summary>
		public void Reset()
		{
			digest.Reset();
		}

		private byte[] DerEncode(
			BigInteger	r,
			BigInteger	s)
		{
			return new DerSequence(new DerInteger(r), new DerInteger(s)).GetDerEncoded();
		}

		private BigInteger[] DerDecode(
			byte[] encoding)
		{
			Asn1Sequence s = (Asn1Sequence) Asn1Object.FromByteArray(encoding);

			return new BigInteger[]
			{
				((DerInteger) s[0]).Value,
				((DerInteger) s[1]).Value
			};
		}
	}
}
