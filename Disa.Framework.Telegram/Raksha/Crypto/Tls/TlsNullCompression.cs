using System;
using System.IO;

namespace Raksha.Crypto.Tls
{
	public class TlsNullCompression
		: ITlsCompression
	{
		public virtual Stream Compress(Stream output)
		{
			return output;
		}

		public virtual Stream Decompress(Stream output)
		{
			return output;
		}
	}
}
