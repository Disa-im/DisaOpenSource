using System;
using System.IO;
using BigMath;
using Raksha.Math;

namespace Raksha.Bcpg
{
	/// <remarks>A multiple precision integer</remarks>
    public class MPInteger
        : BcpgObject
    {
        private readonly BigInteger val;

        public MPInteger(
            BcpgInputStream bcpgIn)
        {
			if (bcpgIn == null)
				throw new ArgumentNullException("bcpgIn");

			int length = (bcpgIn.ReadByte() << 8) | bcpgIn.ReadByte();
            byte[] bytes = new byte[(length + 7) / 8];

            bcpgIn.ReadFully(bytes);

            this.val = new BigInteger(1, bytes);
        }

		public MPInteger(
            BigInteger val)
        {
			if (val == null)
				throw new ArgumentNullException("val");
			if (val.Sign < 0)
				throw new ArgumentException("Values must be positive", "val");

			this.val = val;
        }

		public BigInteger Value
        {
            get { return val; }
        }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
			bcpgOut.WriteShort((short) val.BitLength);
			bcpgOut.Write(val.ToByteArrayUnsigned());
        }

		internal static void Encode(
			BcpgOutputStream	bcpgOut,
			BigInteger			val)
		{
			bcpgOut.WriteShort((short) val.BitLength);
			bcpgOut.Write(val.ToByteArrayUnsigned());
		}
    }
}
