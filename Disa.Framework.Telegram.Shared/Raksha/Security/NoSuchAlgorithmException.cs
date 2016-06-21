using System;

namespace Raksha.Security
{
	[Obsolete("Never thrown")]
	public class NoSuchAlgorithmException : GeneralSecurityException
	{
		public NoSuchAlgorithmException() : base() {}
		public NoSuchAlgorithmException(string message) : base(message) {}
		public NoSuchAlgorithmException(string message, Exception exception) : base(message, exception) {}
	}
}
