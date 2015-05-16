using System;
using Raksha.Asn1.X509;

namespace Raksha.Crypto.Tls
{
	/// <remarks>
	/// This should be implemented by any class which can find out, if a given
	/// certificate chain is being accepted by an client.
	/// </remarks>
	[Obsolete("Perform certificate verification in TlsAuthentication implementation")]
	public interface ICertificateVerifyer
	{
		/// <param name="certs">The certs, which are part of the chain.</param>
		/// <returns>True, if the chain is accepted, false otherwise</returns>
		bool IsValid(X509CertificateStructure[] certs);
	}
}
