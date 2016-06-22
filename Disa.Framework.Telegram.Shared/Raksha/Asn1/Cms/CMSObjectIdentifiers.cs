using Raksha.Asn1.Pkcs;

namespace Raksha.Asn1.Cms
{
    public abstract class CmsObjectIdentifiers
    {
        public static readonly DerObjectIdentifier Data = PkcsObjectIdentifiers.Data;
        public static readonly DerObjectIdentifier SignedData = PkcsObjectIdentifiers.SignedData;
        public static readonly DerObjectIdentifier EnvelopedData = PkcsObjectIdentifiers.EnvelopedData;
        public static readonly DerObjectIdentifier SignedAndEnvelopedData = PkcsObjectIdentifiers.SignedAndEnvelopedData;
        public static readonly DerObjectIdentifier DigestedData = PkcsObjectIdentifiers.DigestedData;
        public static readonly DerObjectIdentifier EncryptedData = PkcsObjectIdentifiers.EncryptedData;
		public static readonly DerObjectIdentifier AuthenticatedData = PkcsObjectIdentifiers.IdCTAuthData;
        public static readonly DerObjectIdentifier CompressedData = PkcsObjectIdentifiers.IdCTCompressedData;
		public static readonly DerObjectIdentifier AuthEnvelopedData = PkcsObjectIdentifiers.IdCTAuthEnvelopedData;
		public static readonly DerObjectIdentifier timestampedData = PkcsObjectIdentifiers.IdCTTimestampedData;
    }
}
