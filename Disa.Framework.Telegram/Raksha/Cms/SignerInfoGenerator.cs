using System;
using Raksha.Asn1;
using Raksha.Asn1.Cms;
using Raksha.Asn1.X509;

namespace Raksha.Cms
{
	internal interface SignerInfoGenerator
	{
		SignerInfo Generate(DerObjectIdentifier contentType, AlgorithmIdentifier digestAlgorithm,
        	byte[] calculatedDigest);
	}
}
