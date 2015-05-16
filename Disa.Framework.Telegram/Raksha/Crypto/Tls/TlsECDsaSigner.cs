using System;
using Raksha.Crypto.Parameters;
using Raksha.Crypto.Signers;

namespace Raksha.Crypto.Tls
{
	internal class TlsECDsaSigner
		: TlsDsaSigner
	{
		public override bool IsValidPublicKey(AsymmetricKeyParameter publicKey)
		{
			return publicKey is ECPublicKeyParameters;
		}

		protected override IDsa CreateDsaImpl()
		{
			return new ECDsaSigner();
		}
	}
}
