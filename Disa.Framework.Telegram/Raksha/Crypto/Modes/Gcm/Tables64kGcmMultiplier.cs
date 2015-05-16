using System;
using Raksha.Crypto.Utilities;

namespace Raksha.Crypto.Modes.Gcm
{
	public class Tables64kGcmMultiplier
		: IGcmMultiplier
	{
		private readonly uint[][][] M = new uint[16][][];

		public void Init(byte[] H)
		{
			M[0] = new uint[256][];
			M[0][0] = new uint[4];
			M[0][128] = GcmUtilities.AsUints(H);
			for (int j = 64; j >= 1; j >>= 1)
			{
				uint[] tmp = (uint[])M[0][j + j].Clone();
				GcmUtilities.MultiplyP(tmp);
				M[0][j] = tmp;
			}
			for (int i = 0;;)
			{
				for (int j = 2; j < 256; j += j)
				{
					for (int k = 1; k < j; ++k)
					{
						uint[] tmp = (uint[])M[i][j].Clone();
						GcmUtilities.Xor(tmp, M[i][k]);
						M[i][j + k] = tmp;
					}
				}

				if (++i == 16) return;

				M[i] = new uint[256][];
				M[i][0] = new uint[4];
				for (int j = 128; j > 0; j >>= 1)
				{
					uint[] tmp = (uint[])M[i - 1][j].Clone();
					GcmUtilities.MultiplyP8(tmp);
					M[i][j] = tmp;
				}
			}
		}

		public void MultiplyH(byte[] x)
		{
			uint[] z = new uint[4];
			for (int i = 0; i != 16; ++i)
			{
				//GcmUtilities.Xor(z, M[i][x[i]]);
				uint[] m = M[i][x[i]];
				z[0] ^= m[0];
				z[1] ^= m[1];
				z[2] ^= m[2];
				z[3] ^= m[3];
			}

			Pack.UInt32_To_BE(z[0], x, 0);
			Pack.UInt32_To_BE(z[1], x, 4);
			Pack.UInt32_To_BE(z[2], x, 8);
			Pack.UInt32_To_BE(z[3], x, 12);
		}
	}
}
