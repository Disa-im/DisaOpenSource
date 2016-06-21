using System;
using Raksha.Asn1.X509;

namespace Raksha.Crypto.Tls
{
	/// <remarks>
	/// A certificate verifyer, that will always return true.
	/// <pre>
	/// DO NOT USE THIS FILE UNLESS YOU KNOW EXACTLY WHAT YOU ARE DOING.
	/// </pre>
	/// </remarks>
	[Obsolete("Perform certificate verification in TlsAuthentication implementation")]
	public class AlwaysValidVerifyer
		: ICertificateVerifyer
	{
		/// <summary>Return true.</summary>
		public bool IsValid(
			X509CertificateStructure[] certs)
		{
			return true;
		}
	}
}
