using System;
using Raksha.Asn1;

namespace Raksha.Crypto.Agreement.Kdf
{
	public class DHKdfParameters
		: IDerivationParameters
	{
		private readonly DerObjectIdentifier algorithm;
		private readonly int keySize;
		private readonly byte[] z;
		private readonly byte[] extraInfo;

		public DHKdfParameters(
			DerObjectIdentifier	algorithm,
			int					keySize,
			byte[]				z)
			: this(algorithm, keySize, z, null)
		{
		}

		public DHKdfParameters(
			DerObjectIdentifier algorithm,
			int keySize,
			byte[] z,
			byte[] extraInfo)
		{
			this.algorithm = algorithm;
			this.keySize = keySize;
			this.z = z; // TODO Clone?
			this.extraInfo = extraInfo;
		}

		public DerObjectIdentifier Algorithm
		{
			get { return algorithm; }
		}

		public int KeySize
		{
			get { return keySize; }
		}

		public byte[] GetZ()
		{
			// TODO Clone?
			return z;
		}

		public byte[] GetExtraInfo()
		{
			// TODO Clone?
			return extraInfo;
		}
	}
}
