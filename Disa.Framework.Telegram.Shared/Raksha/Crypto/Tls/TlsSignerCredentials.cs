using System;
using System.IO;

namespace Raksha.Crypto.Tls
{
	public interface TlsSignerCredentials : TlsCredentials
	{
		/// <exception cref="IOException"></exception>
		byte[] GenerateCertificateSignature(byte[] md5andsha1);
	}
}
