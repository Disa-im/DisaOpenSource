// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CMSSignedDataStreamGenerator.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.IO;
using Raksha.Asn1;
using Raksha.Asn1.Cms;
using Raksha.Asn1.X509;
using Raksha.Crypto;
using Raksha.Crypto.Parameters;
using Raksha.Security;
using Raksha.Utilities;
using Raksha.Utilities.Collections;
using Raksha.Utilities.IO;
using Raksha.X509;
using AttributeTable = Raksha.Asn1.Cms.AttributeTable;

namespace Raksha.Cms
{
    /**
    * General class for generating a pkcs7-signature message stream.
    * <p>
    * A simple example of usage.
    * </p>
    * <pre>
    *      IX509Store                   certs...
    *      CmsSignedDataStreamGenerator gen = new CmsSignedDataStreamGenerator();
    *
    *      gen.AddSigner(privateKey, cert, CmsSignedDataStreamGenerator.DIGEST_SHA1);
    *
    *      gen.AddCertificates(certs);
    *
    *      Stream sigOut = gen.Open(bOut);
    *
    *      sigOut.Write(Encoding.UTF8.GetBytes("Hello World!"));
    *
    *      sigOut.Close();
    * </pre>
    */

    public class CmsSignedDataStreamGenerator : CmsSignedGenerator
    {
        private static readonly CmsSignedHelper Helper = CmsSignedHelper.Instance;

        private readonly ISet _messageDigestOids = new HashSet();
        private readonly IDictionary _messageDigests = Platform.CreateHashtable();
        private readonly IDictionary _messageHashes = Platform.CreateHashtable();
        private readonly IList _signerInfs = Platform.CreateArrayList();
        private int _bufferSize;
        private bool _messageDigestsLocked;

        public CmsSignedDataStreamGenerator()
        {
        }

        /// <summary>Constructor allowing specific source of randomness</summary>
        /// <param name="rand">Instance of <c>SecureRandom</c> to use.</param>
        public CmsSignedDataStreamGenerator(SecureRandom rand) : base(rand)
        {
        }

        /**
        * Set the underlying string size for encapsulated data
        *
        * @param bufferSize length of octet strings to buffer the data.
        */

        public void SetBufferSize(int bufferSize)
        {
            _bufferSize = bufferSize;
        }

        public void AddDigests(params string[] digestOids)
        {
            AddDigests((IEnumerable) digestOids);
        }

        public void AddDigests(IEnumerable digestOids)
        {
            foreach (string digestOid in digestOids)
            {
                ConfigureDigest(digestOid);
            }
        }

        /**
        * add a signer - no attributes other than the default ones will be
        * provided here.
        * @throws NoSuchAlgorithmException
        * @throws InvalidKeyException
        */

        public void AddSigner(AsymmetricKeyParameter privateKey, X509Certificate cert, string digestOid)
        {
            AddSigner(privateKey, cert, digestOid, new DefaultSignedAttributeTableGenerator(), null);
        }

        /**
         * add a signer, specifying the digest encryption algorithm - no attributes other than the default ones will be
         * provided here.
         * @throws NoSuchProviderException
         * @throws NoSuchAlgorithmException
         * @throws InvalidKeyException
         */

        public void AddSigner(AsymmetricKeyParameter privateKey, X509Certificate cert, string encryptionOid, string digestOid)
        {
            AddSigner(privateKey, cert, encryptionOid, digestOid, new DefaultSignedAttributeTableGenerator(), null);
        }

        /**
        * add a signer with extra signed/unsigned attributes.
        * @throws NoSuchAlgorithmException
        * @throws InvalidKeyException
        */

        public void AddSigner(AsymmetricKeyParameter privateKey, X509Certificate cert, string digestOid, Asn1.Cms.AttributeTable signedAttr, Asn1.Cms.AttributeTable unsignedAttr)
        {
            AddSigner(privateKey, cert, digestOid, new DefaultSignedAttributeTableGenerator(signedAttr), new SimpleAttributeTableGenerator(unsignedAttr));
        }

        /**
         * add a signer with extra signed/unsigned attributes - specifying digest
         * encryption algorithm.
         * @throws NoSuchProviderException
         * @throws NoSuchAlgorithmException
         * @throws InvalidKeyException
         */

        public void AddSigner(AsymmetricKeyParameter privateKey, X509Certificate cert, string encryptionOid, string digestOid, Asn1.Cms.AttributeTable signedAttr,
            Asn1.Cms.AttributeTable unsignedAttr)
        {
            AddSigner(privateKey, cert, encryptionOid, digestOid, new DefaultSignedAttributeTableGenerator(signedAttr), new SimpleAttributeTableGenerator(unsignedAttr));
        }

        public void AddSigner(AsymmetricKeyParameter privateKey, X509Certificate cert, string digestOid, CmsAttributeTableGenerator signedAttrGenerator,
            CmsAttributeTableGenerator unsignedAttrGenerator)
        {
            AddSigner(privateKey, cert, GetEncOid(privateKey, digestOid), digestOid, signedAttrGenerator, unsignedAttrGenerator);
        }

        public void AddSigner(AsymmetricKeyParameter privateKey, X509Certificate cert, string encryptionOid, string digestOid,
            CmsAttributeTableGenerator signedAttrGenerator, CmsAttributeTableGenerator unsignedAttrGenerator)
        {
            DoAddSigner(privateKey, GetSignerIdentifier(cert), encryptionOid, digestOid, signedAttrGenerator, unsignedAttrGenerator);
        }

        /**
        * add a signer - no attributes other than the default ones will be
        * provided here.
        * @throws NoSuchAlgorithmException
        * @throws InvalidKeyException
        */

        public void AddSigner(AsymmetricKeyParameter privateKey, byte[] subjectKeyID, string digestOid)
        {
            AddSigner(privateKey, subjectKeyID, digestOid, new DefaultSignedAttributeTableGenerator(), null);
        }

        /**
         * add a signer - no attributes other than the default ones will be
         * provided here.
         * @throws NoSuchProviderException
         * @throws NoSuchAlgorithmException
         * @throws InvalidKeyException
         */

        public void AddSigner(AsymmetricKeyParameter privateKey, byte[] subjectKeyID, string encryptionOid, string digestOid)
        {
            AddSigner(privateKey, subjectKeyID, encryptionOid, digestOid, new DefaultSignedAttributeTableGenerator(), null);
        }

        /**
        * add a signer with extra signed/unsigned attributes.
        * @throws NoSuchAlgorithmException
        * @throws InvalidKeyException
        */

        public void AddSigner(AsymmetricKeyParameter privateKey, byte[] subjectKeyID, string digestOid, Asn1.Cms.AttributeTable signedAttr, Asn1.Cms.AttributeTable unsignedAttr)
        {
            AddSigner(privateKey, subjectKeyID, digestOid, new DefaultSignedAttributeTableGenerator(signedAttr), new SimpleAttributeTableGenerator(unsignedAttr));
        }

        public void AddSigner(AsymmetricKeyParameter privateKey, byte[] subjectKeyID, string digestOid, CmsAttributeTableGenerator signedAttrGenerator,
            CmsAttributeTableGenerator unsignedAttrGenerator)
        {
            AddSigner(privateKey, subjectKeyID, GetEncOid(privateKey, digestOid), digestOid, signedAttrGenerator, unsignedAttrGenerator);
        }

        public void AddSigner(AsymmetricKeyParameter privateKey, byte[] subjectKeyID, string encryptionOid, string digestOid,
            CmsAttributeTableGenerator signedAttrGenerator, CmsAttributeTableGenerator unsignedAttrGenerator)
        {
            DoAddSigner(privateKey, GetSignerIdentifier(subjectKeyID), encryptionOid, digestOid, signedAttrGenerator, unsignedAttrGenerator);
        }

        private void DoAddSigner(AsymmetricKeyParameter privateKey, SignerIdentifier signerIdentifier, string encryptionOid, string digestOid,
            CmsAttributeTableGenerator signedAttrGenerator, CmsAttributeTableGenerator unsignedAttrGenerator)
        {
            ConfigureDigest(digestOid);

            var signerInf = new SignerInfoGeneratorImpl(this, privateKey, signerIdentifier, digestOid, encryptionOid, signedAttrGenerator, unsignedAttrGenerator);

            _signerInfs.Add(new DigestAndSignerInfoGeneratorHolder(signerInf, digestOid));
        }

        internal override void AddSignerCallback(SignerInformation si)
        {
            // FIXME If there were parameters in si.DigestAlgorithmID.Parameters, they are lost
            // NB: Would need to call FixAlgID on the DigestAlgorithmID

            // For precalculated signers, just need to register the algorithm, not configure a digest
            RegisterDigestOid(si.DigestAlgorithmID.ObjectID.Id);
        }

        /**
        * generate a signed object that for a CMS Signed Data
        * object - if encapsulate is true a copy
        * of the message will be included in the signature with the
        * default content type "data".
        */

        public Stream Open(Stream outStream, bool encapsulate = false)
        {
            return Open(outStream, Data, encapsulate);
        }

        /**
         * generate a signed object that for a CMS Signed Data
         * object using the given provider - if encapsulate is true a copy
         * of the message will be included in the signature with the
         * default content type "data". If dataOutputStream is non null the data
         * being signed will be written to the stream as it is processed.
         * @param out stream the CMS object is to be written to.
         * @param encapsulate true if data should be encapsulated.
         * @param dataOutputStream output stream to copy the data being signed to.
         */

        public Stream Open(Stream outStream, bool encapsulate, Stream dataOutputStream)
        {
            return Open(outStream, Data, encapsulate, dataOutputStream);
        }

        /**
        * generate a signed object that for a CMS Signed Data
        * object - if encapsulate is true a copy
        * of the message will be included in the signature. The content type
        * is set according to the OID represented by the string signedContentType.
        */

        public Stream Open(Stream outStream, string signedContentType, bool encapsulate)
        {
            return Open(outStream, signedContentType, encapsulate, null);
        }

        /**
        * generate a signed object that for a CMS Signed Data
        * object using the given provider - if encapsulate is true a copy
        * of the message will be included in the signature. The content type
        * is set according to the OID represented by the string signedContentType.
        * @param out stream the CMS object is to be written to.
        * @param signedContentType OID for data to be signed.
        * @param encapsulate true if data should be encapsulated.
        * @param dataOutputStream output stream to copy the data being signed to.
        */

        public Stream Open(Stream outStream, string signedContentType, bool encapsulate, Stream dataOutputStream)
        {
            if (outStream == null)
            {
                throw new ArgumentNullException("outStream");
            }
            if (!outStream.CanWrite)
            {
                throw new ArgumentException("Expected writeable stream", "outStream");
            }
            if (dataOutputStream != null && !dataOutputStream.CanWrite)
            {
                throw new ArgumentException("Expected writeable stream", "dataOutputStream");
            }

            _messageDigestsLocked = true;

            //
            // ContentInfo
            //
            var sGen = new BerSequenceGenerator(outStream);

            sGen.AddObject(CmsObjectIdentifiers.SignedData);

            //
            // Signed Data
            //
            var sigGen = new BerSequenceGenerator(sGen.GetRawOutputStream(), 0, true);

            bool isCounterSignature = (signedContentType == null);

            DerObjectIdentifier contentTypeOid = isCounterSignature ? null : new DerObjectIdentifier(signedContentType);

            sigGen.AddObject(CalculateVersion(contentTypeOid));

            var digestAlgs = new Asn1EncodableVector();

            foreach (string digestOid in _messageDigestOids)
            {
                digestAlgs.Add(new AlgorithmIdentifier(new DerObjectIdentifier(digestOid), DerNull.Instance));
            }

            {
                byte[] tmp = new DerSet(digestAlgs).GetEncoded();
                sigGen.GetRawOutputStream().Write(tmp, 0, tmp.Length);
            }

            var eiGen = new BerSequenceGenerator(sigGen.GetRawOutputStream());
            eiGen.AddObject(contentTypeOid);

            // If encapsulating, add the data as an octet string in the sequence
            Stream encapStream = encapsulate ? CmsUtilities.CreateBerOctetOutputStream(eiGen.GetRawOutputStream(), 0, true, _bufferSize) : null;

            // Also send the data to 'dataOutputStream' if necessary
            Stream teeStream = GetSafeTeeOutputStream(dataOutputStream, encapStream);

            // Let all the digests see the data as it is written
            Stream digStream = AttachDigestsToOutputStream(_messageDigests.Values, teeStream);

            return new CmsSignedDataOutputStream(this, digStream, signedContentType, sGen, sigGen, eiGen);
        }

        private void RegisterDigestOid(string digestOid)
        {
            if (_messageDigestsLocked)
            {
                if (!_messageDigestOids.Contains(digestOid))
                {
                    throw new InvalidOperationException("Cannot register new digest OIDs after the data stream is opened");
                }
            }
            else
            {
                _messageDigestOids.Add(digestOid);
            }
        }

        private void ConfigureDigest(string digestOid)
        {
            RegisterDigestOid(digestOid);

            string digestName = Helper.GetDigestAlgName(digestOid);
            var dig = (IDigest) _messageDigests[digestName];
            if (dig == null)
            {
                if (_messageDigestsLocked)
                {
                    throw new InvalidOperationException("Cannot configure new digests after the data stream is opened");
                }

                dig = Helper.GetDigestInstance(digestName);
                _messageDigests[digestName] = dig;
            }
        }

        // TODO Make public?
        internal void Generate(Stream outStream, string eContentType, bool encapsulate, Stream dataOutputStream, CmsProcessable content)
        {
            using (Stream signedOut = Open(outStream, eContentType, encapsulate, dataOutputStream))
            {
                if (content != null)
                {
                    content.Write(signedOut);
                }
            }
        }

        // RFC3852, section 5.1:
        // IF ((certificates is present) AND
        //    (any certificates with a type of other are present)) OR
        //    ((crls is present) AND
        //    (any crls with a type of other are present))
        // THEN version MUST be 5
        // ELSE
        //    IF (certificates is present) AND
        //       (any version 2 attribute certificates are present)
        //    THEN version MUST be 4
        //    ELSE
        //       IF ((certificates is present) AND
        //          (any version 1 attribute certificates are present)) OR
        //          (any SignerInfo structures are version 3) OR
        //          (encapContentInfo eContentType is other than id-data)
        //       THEN version MUST be 3
        //       ELSE version MUST be 1
        //
        private DerInteger CalculateVersion(DerObjectIdentifier contentOid)
        {
            bool otherCert = false;
            bool otherCrl = false;
            bool attrCertV1Found = false;
            bool attrCertV2Found = false;

            if (_certs != null)
            {
                foreach (object obj in _certs)
                {
                    if (obj is Asn1TaggedObject)
                    {
                        var tagged = (Asn1TaggedObject) obj;

                        if (tagged.TagNo == 1)
                        {
                            attrCertV1Found = true;
                        }
                        else if (tagged.TagNo == 2)
                        {
                            attrCertV2Found = true;
                        }
                        else if (tagged.TagNo == 3)
                        {
                            otherCert = true;
                            break;
                        }
                    }
                }
            }

            if (otherCert)
            {
                return new DerInteger(5);
            }

            if (_crls != null)
            {
                foreach (object obj in _crls)
                {
                    if (obj is Asn1TaggedObject)
                    {
                        otherCrl = true;
                        break;
                    }
                }
            }

            if (otherCrl)
            {
                return new DerInteger(5);
            }

            if (attrCertV2Found)
            {
                return new DerInteger(4);
            }

            if (attrCertV1Found || !CmsObjectIdentifiers.Data.Equals(contentOid) || CheckForVersion3(_signers))
            {
                return new DerInteger(3);
            }

            return new DerInteger(1);
        }

        private bool CheckForVersion3(IList signerInfos)
        {
            foreach (SignerInformation si in signerInfos)
            {
                SignerInfo s = SignerInfo.GetInstance(si.ToSignerInfo());

                if (s.Version.Value.IntValue == 3)
                {
                    return true;
                }
            }

            return false;
        }

        private static Stream AttachDigestsToOutputStream(ICollection digests, Stream s)
        {
            Stream result = s;
            foreach (IDigest digest in digests)
            {
                result = GetSafeTeeOutputStream(result, new DigOutputStream(digest));
            }
            return result;
        }

        private static Stream GetSafeOutputStream(Stream s)
        {
            if (s == null)
            {
                return new NullOutputStream();
            }
            return s;
        }

        private static Stream GetSafeTeeOutputStream(Stream s1, Stream s2)
        {
            if (s1 == null)
            {
                return GetSafeOutputStream(s2);
            }
            if (s2 == null)
            {
                return GetSafeOutputStream(s1);
            }
            return new TeeOutputStream(s1, s2);
        }

        private class CmsSignedDataOutputStream : BaseOutputStream
        {
            private readonly DerObjectIdentifier _contentOID;
            private readonly BerSequenceGenerator _eiGen;
            private readonly Stream _out;
            private readonly CmsSignedDataStreamGenerator _outer;
            private readonly BerSequenceGenerator _sGen;
            private readonly BerSequenceGenerator _sigGen;

            public CmsSignedDataOutputStream(CmsSignedDataStreamGenerator outer, Stream outStream, string contentOID, BerSequenceGenerator sGen,
                BerSequenceGenerator sigGen, BerSequenceGenerator eiGen)
            {
                _outer = outer;

                _out = outStream;
                _contentOID = new DerObjectIdentifier(contentOID);
                _sGen = sGen;
                _sigGen = sigGen;
                _eiGen = eiGen;
            }

            public override void WriteByte(byte b)
            {
                _out.WriteByte(b);
            }

            public override void Write(byte[] bytes, int off, int len)
            {
                _out.Write(bytes, off, len);
            }

            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (!disposing)
                    {
                        return;
                    }

                    _out.Dispose();

                    // TODO Parent context(s) should really be be closed explicitly

                    _eiGen.Close();

                    _outer._digests.Clear(); // clear the current preserved digest state

                    if (_outer._certs.Count > 0)
                    {
                        Asn1Set certs = CmsUtilities.CreateBerSetFromList(_outer._certs);

                        WriteToGenerator(_sigGen, new BerTaggedObject(false, 0, certs));
                    }

                    if (_outer._crls.Count > 0)
                    {
                        Asn1Set crls = CmsUtilities.CreateBerSetFromList(_outer._crls);

                        WriteToGenerator(_sigGen, new BerTaggedObject(false, 1, crls));
                    }

                    //
                    // Calculate the digest hashes
                    //
                    foreach (DictionaryEntry de in _outer._messageDigests)
                    {
                        _outer._messageHashes.Add(de.Key, DigestUtilities.DoFinal((IDigest) de.Value));
                    }

                    // TODO If the digest OIDs for precalculated signers weren't mixed in with
                    // the others, we could fill in outer._digests here, instead of SignerInfoGenerator.Generate

                    //
                    // collect all the SignerInfo objects
                    //
                    var signerInfos = new Asn1EncodableVector();

                    //
                    // add the generated SignerInfo objects
                    //
                    {
                        foreach (DigestAndSignerInfoGeneratorHolder holder in _outer._signerInfs)
                        {
                            AlgorithmIdentifier digestAlgorithm = holder.DigestAlgorithm;

                            var calculatedDigest = (byte[]) _outer._messageHashes[Helper.GetDigestAlgName(holder.DigestOID)];
                            _outer._digests[holder.DigestOID] = calculatedDigest.Clone();

                            signerInfos.Add(holder.SignerInf.Generate(_contentOID, digestAlgorithm, calculatedDigest));
                        }
                    }

                    //
                    // add the precalculated SignerInfo objects.
                    //
                    {
                        foreach (SignerInformation signer in _outer._signers)
                        {
                            // TODO Verify the content type and calculated digest match the precalculated SignerInfo
                            //						if (!signer.ContentType.Equals(_contentOID))
                            //						{
                            //							// TODO The precalculated content type did not match - error?
                            //						}
                            //
                            //						byte[] calculatedDigest = (byte[])outer._digests[signer.DigestAlgOid];
                            //						if (calculatedDigest == null)
                            //						{
                            //							// TODO We can't confirm this digest because we didn't calculate it - error?
                            //						}
                            //						else
                            //						{
                            //							if (!Arrays.AreEqual(signer.GetContentDigest(), calculatedDigest))
                            //							{
                            //								// TODO The precalculated digest did not match - error?
                            //							}
                            //						}

                            signerInfos.Add(signer.ToSignerInfo());
                        }
                    }

                    WriteToGenerator(_sigGen, new DerSet(signerInfos));

                    _sigGen.Close();
                    _sGen.Close();
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }

            private static void WriteToGenerator(Asn1Generator ag, Asn1Encodable ae)
            {
                byte[] encoded = ae.GetEncoded();
                ag.GetRawOutputStream().Write(encoded, 0, encoded.Length);
            }
        }

        private class DigestAndSignerInfoGeneratorHolder
        {
            internal readonly string DigestOID;
            internal readonly SignerInfoGenerator SignerInf;

            internal DigestAndSignerInfoGeneratorHolder(SignerInfoGenerator signerInf, String digestOID)
            {
                SignerInf = signerInf;
                DigestOID = digestOID;
            }

            internal AlgorithmIdentifier DigestAlgorithm
            {
                get { return new AlgorithmIdentifier(new DerObjectIdentifier(DigestOID), DerNull.Instance); }
            }
        }

        private class SignerInfoGeneratorImpl : SignerInfoGenerator
        {
            private readonly string _digestOID;
            private readonly string _encName;
            private readonly string _encOID;
            private readonly CmsSignedDataStreamGenerator _outer;
            private readonly CmsAttributeTableGenerator _sAttr;
            private readonly ISigner _sig;
            private readonly SignerIdentifier _signerIdentifier;
            private readonly CmsAttributeTableGenerator _unsAttr;

            internal SignerInfoGeneratorImpl(CmsSignedDataStreamGenerator outer, AsymmetricKeyParameter key, SignerIdentifier signerIdentifier, string digestOID,
                string encOID, CmsAttributeTableGenerator sAttr, CmsAttributeTableGenerator unsAttr)
            {
                _outer = outer;

                _signerIdentifier = signerIdentifier;
                _digestOID = digestOID;
                _encOID = encOID;
                _sAttr = sAttr;
                _unsAttr = unsAttr;
                _encName = Helper.GetEncryptionAlgName(_encOID);

                string digestName = Helper.GetDigestAlgName(_digestOID);
                string signatureName = digestName + "with" + _encName;

                if (_sAttr != null)
                {
                    _sig = Helper.GetSignatureInstance(signatureName);
                }
                else
                {
                    // Note: Need to use raw signatures here since we have already calculated the digest
                    if (_encName.Equals("RSA"))
                    {
                        _sig = Helper.GetSignatureInstance("RSA");
                    }
                    else if (_encName.Equals("DSA"))
                    {
                        _sig = Helper.GetSignatureInstance("NONEwithDSA");
                    }
                        // TODO Add support for raw PSS
//					else if (_encName.equals("RSAandMGF1"))
//					{
//						_sig = CMSSignedHelper.INSTANCE.getSignatureInstance("NONEWITHRSAPSS", _sigProvider);
//						try
//						{
//							// Init the params this way to avoid having a 'raw' version of each PSS algorithm
//							Signature sig2 = CMSSignedHelper.INSTANCE.getSignatureInstance(signatureName, _sigProvider);
//							PSSParameterSpec spec = (PSSParameterSpec)sig2.getParameters().getParameterSpec(PSSParameterSpec.class);
//							_sig.setParameter(spec);
//						}
//						catch (Exception e)
//						{
//							throw new SignatureException("algorithm: " + _encName + " could not be configured.");
//						}
//					}
                    else
                    {
                        throw new SignatureException("algorithm: " + _encName + " not supported in base signatures.");
                    }
                }

                _sig.Init(true, new ParametersWithRandom(key, outer.rand));
            }

            public SignerInfo Generate(DerObjectIdentifier contentType, AlgorithmIdentifier digestAlgorithm, byte[] calculatedDigest)
            {
                try
                {
                    string digestName = Helper.GetDigestAlgName(_digestOID);
                    string signatureName = digestName + "with" + _encName;

//					AlgorithmIdentifier digAlgId = DigestAlgorithmID;
//
//					byte[] hash = (byte[])outer._messageHashes[Helper.GetDigestAlgName(this._digestOID)];
//					outer._digests[_digestOID] = hash.Clone();

                    byte[] bytesToSign = calculatedDigest;

                    /* RFC 3852 5.4
                     * The result of the message digest calculation process depends on
                     * whether the signedAttrs field is present.  When the field is absent,
                     * the result is just the message digest of the content as described
                     *
                     * above.  When the field is present, however, the result is the message
                     * digest of the complete DER encoding of the SignedAttrs value
                     * contained in the signedAttrs field.
                     */
                    Asn1Set signedAttr = null;
                    if (_sAttr != null)
                    {
                        IDictionary parameters = _outer.GetBaseParameters(contentType, digestAlgorithm, calculatedDigest);

//						Asn1.Cms.AttributeTable signed = _sAttr.GetAttributes(Collections.unmodifiableMap(parameters));
                        Asn1.Cms.AttributeTable signed = _sAttr.GetAttributes(parameters);

                        if (contentType == null) //counter signature
                        {
                            if (signed != null && signed[CmsAttributes.ContentType] != null)
                            {
                                IDictionary tmpSigned = signed.ToDictionary();
                                tmpSigned.Remove(CmsAttributes.ContentType);
                                signed = new Asn1.Cms.AttributeTable(tmpSigned);
                            }
                        }

                        signedAttr = _outer.GetAttributeSet(signed);

                        // sig must be composed from the DER encoding.
                        bytesToSign = signedAttr.GetEncoded(Asn1Encodable.Der);
                    }
                    else
                    {
                        // Note: Need to use raw signatures here since we have already calculated the digest
                        if (_encName.Equals("RSA"))
                        {
                            var dInfo = new DigestInfo(digestAlgorithm, calculatedDigest);
                            bytesToSign = dInfo.GetEncoded(Asn1Encodable.Der);
                        }
                    }

                    _sig.BlockUpdate(bytesToSign, 0, bytesToSign.Length);
                    byte[] sigBytes = _sig.GenerateSignature();

                    Asn1Set unsignedAttr = null;
                    if (_unsAttr != null)
                    {
                        IDictionary parameters = _outer.GetBaseParameters(contentType, digestAlgorithm, calculatedDigest);
                        parameters[CmsAttributeTableParameter.Signature] = sigBytes.Clone();

//						Asn1.Cms.AttributeTable unsigned = _unsAttr.getAttributes(Collections.unmodifiableMap(parameters));
                        Asn1.Cms.AttributeTable unsigned = _unsAttr.GetAttributes(parameters);

                        unsignedAttr = _outer.GetAttributeSet(unsigned);
                    }

                    // TODO[RSAPSS] Need the ability to specify non-default parameters
                    Asn1Encodable sigX509Parameters = SignerUtilities.GetDefaultX509Parameters(signatureName);
                    AlgorithmIdentifier digestEncryptionAlgorithm = GetEncAlgorithmIdentifier(new DerObjectIdentifier(_encOID), sigX509Parameters);

                    return new SignerInfo(_signerIdentifier, digestAlgorithm, signedAttr, digestEncryptionAlgorithm, new DerOctetString(sigBytes), unsignedAttr);
                }
                catch (IOException e)
                {
                    throw new CmsStreamException("encoding error.", e);
                }
                catch (SignatureException e)
                {
                    throw new CmsStreamException("error creating signature.", e);
                }
            }
        }
    }
}
