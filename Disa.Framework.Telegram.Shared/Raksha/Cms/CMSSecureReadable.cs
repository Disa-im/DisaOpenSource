using System;
using Raksha.Asn1.X509;
using Raksha.Crypto.Parameters;

namespace Raksha.Cms
{
	internal interface CmsSecureReadable
	{
		AlgorithmIdentifier Algorithm { get; }
		object CryptoObject { get; }
		ICmsReadable GetReadable(KeyParameter key);
	}
}
