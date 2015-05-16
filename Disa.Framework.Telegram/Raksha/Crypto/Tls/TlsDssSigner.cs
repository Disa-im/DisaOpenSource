using System;
using Raksha.Crypto.Parameters;
using Raksha.Crypto.Signers;

namespace Raksha.Crypto.Tls
{
	internal class TlsDssSigner
		: TlsDsaSigner
	{
		public override bool IsValidPublicKey(AsymmetricKeyParameter publicKey)
		{
			return publicKey is DsaPublicKeyParameters;
		}

	    protected override IDsa CreateDsaImpl()
	    {
			return new DsaSigner();
	    }
	}
}
