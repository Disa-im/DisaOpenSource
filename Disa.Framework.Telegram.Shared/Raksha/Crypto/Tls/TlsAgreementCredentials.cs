using System;
using System.IO;

namespace Raksha.Crypto.Tls
{
	public interface TlsAgreementCredentials : TlsCredentials
	{
		/// <exception cref="IOException"></exception>
		byte[] GenerateAgreement(AsymmetricKeyParameter serverPublicKey);
	}
}
