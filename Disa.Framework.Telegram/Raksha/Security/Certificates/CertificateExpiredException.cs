using System;

namespace Raksha.Security.Certificates
{
	public class CertificateExpiredException : CertificateException
	{
		public CertificateExpiredException() : base() { }
		public CertificateExpiredException(string message) : base(message) { }
		public CertificateExpiredException(string message, Exception exception) : base(message, exception) { }
	}
}
