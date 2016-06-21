using System;
using System.IO;

namespace Raksha.Security
{
	public class EncryptionException
		: IOException
	{
		public EncryptionException(
			string message)
			: base(message)
		{
		}

		public EncryptionException(
			string		message,
			Exception	exception)
			: base(message, exception)
		{
		}
	}
}
