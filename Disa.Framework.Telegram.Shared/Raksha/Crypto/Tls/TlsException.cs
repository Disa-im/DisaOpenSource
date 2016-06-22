using System;

namespace Raksha.Crypto.Tls
{
	public class TlsException : Exception
	{
		public TlsException() : base() { }
		public TlsException(string message) : base(message) { }
		public TlsException(string message, Exception exception) : base(message, exception) { }
	}
}
