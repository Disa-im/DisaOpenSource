using System;
using Raksha.Security;

namespace Raksha.Crypto.Tls
{
	public interface TlsSigner
	{
    	byte[] CalculateRawSignature(SecureRandom random, AsymmetricKeyParameter privateKey,
			byte[] md5andsha1);
		bool VerifyRawSignature(byte[] sigBytes, AsymmetricKeyParameter publicKey, byte[] md5andsha1);

		ISigner CreateSigner(SecureRandom random, AsymmetricKeyParameter privateKey);
		ISigner CreateVerifyer(AsymmetricKeyParameter publicKey);

		bool IsValidPublicKey(AsymmetricKeyParameter publicKey);
	}
}
