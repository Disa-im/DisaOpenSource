using System;
using System.IO;

namespace Raksha.Bcpg
{
	/// <remarks>Base class for a PGP object.</remarks>
    public abstract class BcpgObject
    {
        public virtual byte[] GetEncoded()
        {
            MemoryStream bOut = new MemoryStream();
            BcpgOutputStream pOut = new BcpgOutputStream(bOut);

			pOut.WriteObject(this);

			return bOut.ToArray();
        }

		public abstract void Encode(BcpgOutputStream bcpgOut);
    }
}

