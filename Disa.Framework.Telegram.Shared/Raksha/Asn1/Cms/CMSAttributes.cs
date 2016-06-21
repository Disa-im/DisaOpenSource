using Raksha.Asn1;
using Raksha.Asn1.Pkcs;

namespace Raksha.Asn1.Cms
{
    public abstract class CmsAttributes
    {
        public static readonly DerObjectIdentifier ContentType		= PkcsObjectIdentifiers.Pkcs9AtContentType;
        public static readonly DerObjectIdentifier MessageDigest	= PkcsObjectIdentifiers.Pkcs9AtMessageDigest;
        public static readonly DerObjectIdentifier SigningTime		= PkcsObjectIdentifiers.Pkcs9AtSigningTime;
		public static readonly DerObjectIdentifier CounterSignature = PkcsObjectIdentifiers.Pkcs9AtCounterSignature;
		public static readonly DerObjectIdentifier ContentHint		= PkcsObjectIdentifiers.IdAAContentHint;
	}
}
