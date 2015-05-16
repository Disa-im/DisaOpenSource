using System;
using Raksha.Crypto.Digests;

namespace Raksha.Crypto.Tls
{
	/// <remarks>A combined hash, which implements md5(m) || sha1(m).</remarks>
	internal class CombinedHash
		: IDigest
	{
		private readonly MD5Digest md5;
		private readonly Sha1Digest sha1;

		internal CombinedHash()
		{
			this.md5 = new MD5Digest();
			this.sha1 = new Sha1Digest();
		}

		internal CombinedHash(CombinedHash t)
		{
			this.md5 = new MD5Digest(t.md5);
			this.sha1 = new Sha1Digest(t.sha1);
		}

		/// <seealso cref="IDigest.AlgorithmName"/>
		public string AlgorithmName
		{
			get
			{
				return md5.AlgorithmName + " and " + sha1.AlgorithmName + " for TLS 1.0";
			}
		}

		/// <seealso cref="IDigest.GetByteLength"/>
		public int GetByteLength()
		{
			return System.Math.Max(md5.GetByteLength(), sha1.GetByteLength());
		}

		/// <seealso cref="IDigest.GetDigestSize"/>
		public int GetDigestSize()
		{
			return md5.GetDigestSize() + sha1.GetDigestSize();
		}

		/// <seealso cref="IDigest.Update"/>
		public void Update(
			byte input)
		{
			md5.Update(input);
			sha1.Update(input);
		}

		/// <seealso cref="IDigest.BlockUpdate"/>
		public void BlockUpdate(
			byte[]	input,
			int		inOff,
			int		len)
		{
			md5.BlockUpdate(input, inOff, len);
			sha1.BlockUpdate(input, inOff, len);
		}

		/// <seealso cref="IDigest.DoFinal"/>
		public int DoFinal(
			byte[]	output,
			int		outOff)
		{
			int i1 = md5.DoFinal(output, outOff);
			int i2 = sha1.DoFinal(output, outOff + i1);
			return i1 + i2;
		}

		/// <seealso cref="IDigest.Reset"/>
		public void Reset()
		{
			md5.Reset();
			sha1.Reset();
		}
	}
}
