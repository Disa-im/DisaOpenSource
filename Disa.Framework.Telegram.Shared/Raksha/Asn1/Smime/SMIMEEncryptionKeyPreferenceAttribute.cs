using Raksha.Asn1;
using Raksha.Asn1.Cms;
using Raksha.Asn1.X509;

namespace Raksha.Asn1.Smime
{
    /**
     * The SmimeEncryptionKeyPreference object.
     * <pre>
     * SmimeEncryptionKeyPreference ::= CHOICE {
     *     issuerAndSerialNumber   [0] IssuerAndSerialNumber,
     *     receipentKeyId          [1] RecipientKeyIdentifier,
     *     subjectAltKeyIdentifier [2] SubjectKeyIdentifier
     * }
     * </pre>
     */
    public class SmimeEncryptionKeyPreferenceAttribute
        : AttributeX509
    {
        public SmimeEncryptionKeyPreferenceAttribute(
            IssuerAndSerialNumber issAndSer)
            : base(SmimeAttributes.EncrypKeyPref,
                new DerSet(new DerTaggedObject(false, 0, issAndSer)))
        {
        }

        public SmimeEncryptionKeyPreferenceAttribute(
            RecipientKeyIdentifier rKeyID)
            : base(SmimeAttributes.EncrypKeyPref,
                new DerSet(new DerTaggedObject(false, 1, rKeyID)))
        {
        }

        /**
         * @param sKeyId the subjectKeyIdentifier value (normally the X.509 one)
         */
        public SmimeEncryptionKeyPreferenceAttribute(
            Asn1OctetString sKeyID)
            : base(SmimeAttributes.EncrypKeyPref,
                new DerSet(new DerTaggedObject(false, 2, sKeyID)))
        {
        }
    }
}
