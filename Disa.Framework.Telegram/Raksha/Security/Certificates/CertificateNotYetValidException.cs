using System;

namespace Raksha.Security.Certificates
{
	public class CertificateNotYetValidException : CertificateException
	{
		public CertificateNotYetValidException() : base() { }
		public CertificateNotYetValidException(string message) : base(message) { }
		public CertificateNotYetValidException(string message, Exception exception) : base(message, exception) { }
	}
}
