using System;

namespace Raksha.Security.Certificates
{
	public class CertificateException : GeneralSecurityException
	{
		public CertificateException() : base() { }
		public CertificateException(string message) : base(message) { }
		public CertificateException(string message, Exception exception) : base(message, exception) { }
	}
}
