// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Pkcs12Store.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Raksha.Asn1;
using Raksha.Asn1.Oiw;
using Raksha.Asn1.Pkcs;
using Raksha.Asn1.Utilities;
using Raksha.Asn1.X509;
using Raksha.Crypto;
using Raksha.Security;
using Raksha.Utilities;
using Raksha.Utilities.Collections;
using Raksha.Utilities.Encoders;
using Raksha.X509;

namespace Raksha.Pkcs
{
    public class Pkcs12Store
    {
        private const int MinIterations = 1024;
        private const int SaltSize = 20;
        private readonly DerObjectIdentifier _certAlgorithm;
        private readonly IgnoresCaseHashtable _certs = new IgnoresCaseHashtable();
        private readonly IDictionary _chainCerts = Platform.CreateHashtable();
        private readonly DerObjectIdentifier _keyAlgorithm;
        private readonly IDictionary _keyCerts = Platform.CreateHashtable();
        private readonly IgnoresCaseHashtable _keys = new IgnoresCaseHashtable();
        private readonly IDictionary _localIds = Platform.CreateHashtable();
        private readonly bool _useDerEncoding;

        internal Pkcs12Store(DerObjectIdentifier keyAlgorithm, DerObjectIdentifier certAlgorithm, bool useDerEncoding)
        {
            _keyAlgorithm = keyAlgorithm;
            _certAlgorithm = certAlgorithm;
            _useDerEncoding = useDerEncoding;
        }

        // TODO Consider making obsolete
//		[Obsolete("Use 'Pkcs12StoreBuilder' instead")]
        public Pkcs12Store() : this(PkcsObjectIdentifiers.PbeWithShaAnd3KeyTripleDesCbc, PkcsObjectIdentifiers.PbewithShaAnd40BitRC2Cbc, false)
        {
        }

        // TODO Consider making obsolete
//		[Obsolete("Use 'Pkcs12StoreBuilder' and 'Load' method instead")]
        public Pkcs12Store(Stream input, char[] password) : this()
        {
            Load(input, password);
        }

        public IEnumerable Aliases
        {
            get { return new EnumerableProxy(GetAliasesTable().Keys); }
        }

        public int Count
        {
            // TODO Seems a little inefficient
            get { return GetAliasesTable().Count; }
        }

        private static SubjectKeyIdentifier CreateSubjectKeyID(AsymmetricKeyParameter pubKey)
        {
            return new SubjectKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pubKey));
        }

        public void Load(Stream input, char[] password)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            var obj = (Asn1Sequence) Asn1Object.FromStream(input);
            var bag = new Pfx(obj);
            ContentInfo info = bag.AuthSafe;
            bool unmarkedKey = false;
            bool wrongPkcs12Zero = false;

            if (bag.MacData != null) // check the mac code
            {
                MacData mData = bag.MacData;
                DigestInfo dInfo = mData.Mac;
                AlgorithmIdentifier algId = dInfo.AlgorithmID;
                byte[] salt = mData.GetSalt();
                int itCount = mData.IterationCount.IntValue;

                byte[] data = ((Asn1OctetString) info.Content).GetOctets();

                byte[] mac = CalculatePbeMac(algId.ObjectID, salt, itCount, password, false, data);
                byte[] dig = dInfo.GetDigest();

                if (!Arrays.ConstantTimeAreEqual(mac, dig))
                {
                    if (password.Length > 0)
                    {
                        throw new IOException("PKCS12 key store MAC invalid - wrong password or corrupted file.");
                    }

                    // Try with incorrect zero length password
                    mac = CalculatePbeMac(algId.ObjectID, salt, itCount, password, true, data);

                    if (!Arrays.ConstantTimeAreEqual(mac, dig))
                    {
                        throw new IOException("PKCS12 key store MAC invalid - wrong password or corrupted file.");
                    }

                    wrongPkcs12Zero = true;
                }
            }

            _keys.Clear();
            _localIds.Clear();

            IList chain = Platform.CreateArrayList();

            if (info.ContentType.Equals(PkcsObjectIdentifiers.Data))
            {
                byte[] octs = ((Asn1OctetString) info.Content).GetOctets();
                var authSafe = new AuthenticatedSafe((Asn1Sequence) Asn1Object.FromByteArray(octs));
                ContentInfo[] cis = authSafe.GetContentInfo();

                foreach (ContentInfo ci in cis)
                {
                    DerObjectIdentifier oid = ci.ContentType;

                    if (oid.Equals(PkcsObjectIdentifiers.Data))
                    {
                        byte[] octets = ((Asn1OctetString) ci.Content).GetOctets();
                        var seq = (Asn1Sequence) Asn1Object.FromByteArray(octets);

                        foreach (Asn1Sequence subSeq in seq)
                        {
                            var b = new SafeBag(subSeq);

                            if (b.BagID.Equals(PkcsObjectIdentifiers.Pkcs8ShroudedKeyBag))
                            {
                                EncryptedPrivateKeyInfo eIn = EncryptedPrivateKeyInfo.GetInstance(b.BagValue);
                                PrivateKeyInfo privInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(password, wrongPkcs12Zero, eIn);
                                AsymmetricKeyParameter privKey = PrivateKeyFactory.CreateKey(privInfo);

                                //
                                // set the attributes on the key
                                //
                                IDictionary attributes = Platform.CreateHashtable();
                                var pkcs12Key = new AsymmetricKeyEntry(privKey, attributes);
                                string alias = null;
                                Asn1OctetString localId = null;

                                if (b.BagAttributes != null)
                                {
                                    foreach (Asn1Sequence sq in b.BagAttributes)
                                    {
                                        var aOid = (DerObjectIdentifier) sq[0];
                                        var attrSet = (Asn1Set) sq[1];
                                        Asn1Encodable attr = null;

                                        if (attrSet.Count > 0)
                                        {
                                            // TODO We should be adding all attributes in the set
                                            attr = attrSet[0];

                                            // TODO We might want to "merge" attribute sets with
                                            // the same OID - currently, differing values give an error
                                            if (attributes.Contains(aOid.Id))
                                            {
                                                // OK, but the value has to be the same
                                                if (!attributes[aOid.Id].Equals(attr))
                                                {
                                                    throw new IOException("attempt to add existing attribute with different value");
                                                }
                                            }
                                            else
                                            {
                                                attributes.Add(aOid.Id, attr);
                                            }

                                            if (aOid.Equals(PkcsObjectIdentifiers.Pkcs9AtFriendlyName))
                                            {
                                                alias = ((DerBmpString) attr).GetString();
                                                // TODO Do these in a separate loop, just collect aliases here
                                                _keys[alias] = pkcs12Key;
                                            }
                                            else if (aOid.Equals(PkcsObjectIdentifiers.Pkcs9AtLocalKeyID))
                                            {
                                                localId = (Asn1OctetString) attr;
                                            }
                                        }
                                    }
                                }

                                if (localId != null)
                                {
                                    string name = Hex.ToHexString(localId.GetOctets());

                                    if (alias == null)
                                    {
                                        _keys[name] = pkcs12Key;
                                    }
                                    else
                                    {
                                        // TODO There may have been more than one alias
                                        _localIds[alias] = name;
                                    }
                                }
                                else
                                {
                                    unmarkedKey = true;
                                    _keys["unmarked"] = pkcs12Key;
                                }
                            }
                            else if (b.BagID.Equals(PkcsObjectIdentifiers.CertBag))
                            {
                                chain.Add(b);
                            }
                            else
                            {
                                Debug.WriteLine("extra " + b.BagID);
                                Debug.WriteLine("extra " + Asn1Dump.DumpAsString(b));
                            }
                        }
                    }
                    else if (oid.Equals(PkcsObjectIdentifiers.EncryptedData))
                    {
                        EncryptedData d = EncryptedData.GetInstance(ci.Content);
                        byte[] octets = CryptPbeData(false, d.EncryptionAlgorithm, password, wrongPkcs12Zero, d.Content.GetOctets());
                        var seq = (Asn1Sequence) Asn1Object.FromByteArray(octets);

                        foreach (Asn1Sequence subSeq in seq)
                        {
                            var b = new SafeBag(subSeq);

                            if (b.BagID.Equals(PkcsObjectIdentifiers.CertBag))
                            {
                                chain.Add(b);
                            }
                            else if (b.BagID.Equals(PkcsObjectIdentifiers.Pkcs8ShroudedKeyBag))
                            {
                                EncryptedPrivateKeyInfo eIn = EncryptedPrivateKeyInfo.GetInstance(b.BagValue);
                                PrivateKeyInfo privInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(password, wrongPkcs12Zero, eIn);
                                AsymmetricKeyParameter privKey = PrivateKeyFactory.CreateKey(privInfo);

                                //
                                // set the attributes on the key
                                //
                                IDictionary attributes = Platform.CreateHashtable();
                                var pkcs12Key = new AsymmetricKeyEntry(privKey, attributes);
                                string alias = null;
                                Asn1OctetString localId = null;

                                foreach (Asn1Sequence sq in b.BagAttributes)
                                {
                                    var aOid = (DerObjectIdentifier) sq[0];
                                    var attrSet = (Asn1Set) sq[1];
                                    Asn1Encodable attr = null;

                                    if (attrSet.Count > 0)
                                    {
                                        // TODO We should be adding all attributes in the set
                                        attr = attrSet[0];

                                        // TODO We might want to "merge" attribute sets with
                                        // the same OID - currently, differing values give an error
                                        if (attributes.Contains(aOid.Id))
                                        {
                                            // OK, but the value has to be the same
                                            if (!attributes[aOid.Id].Equals(attr))
                                            {
                                                throw new IOException("attempt to add existing attribute with different value");
                                            }
                                        }
                                        else
                                        {
                                            attributes.Add(aOid.Id, attr);
                                        }

                                        if (aOid.Equals(PkcsObjectIdentifiers.Pkcs9AtFriendlyName))
                                        {
                                            alias = ((DerBmpString) attr).GetString();
                                            // TODO Do these in a separate loop, just collect aliases here
                                            _keys[alias] = pkcs12Key;
                                        }
                                        else if (aOid.Equals(PkcsObjectIdentifiers.Pkcs9AtLocalKeyID))
                                        {
                                            localId = (Asn1OctetString) attr;
                                        }
                                    }
                                }

                                // TODO Should we be checking localIds != null here
                                // as for PkcsObjectIdentifiers.Data version above?

                                string name = Hex.ToHexString(localId.GetOctets());

                                if (alias == null)
                                {
                                    _keys[name] = pkcs12Key;
                                }
                                else
                                {
                                    // TODO There may have been more than one alias
                                    _localIds[alias] = name;
                                }
                            }
                            else if (b.BagID.Equals(PkcsObjectIdentifiers.KeyBag))
                            {
                                PrivateKeyInfo privKeyInfo = PrivateKeyInfo.GetInstance(b.BagValue);
                                AsymmetricKeyParameter privKey = PrivateKeyFactory.CreateKey(privKeyInfo);

                                //
                                // set the attributes on the key
                                //
                                string alias = null;
                                Asn1OctetString localId = null;
                                IDictionary attributes = Platform.CreateHashtable();
                                var pkcs12Key = new AsymmetricKeyEntry(privKey, attributes);

                                foreach (Asn1Sequence sq in b.BagAttributes)
                                {
                                    var aOid = (DerObjectIdentifier) sq[0];
                                    var attrSet = (Asn1Set) sq[1];
                                    Asn1Encodable attr = null;

                                    if (attrSet.Count > 0)
                                    {
                                        // TODO We should be adding all attributes in the set
                                        attr = attrSet[0];

                                        // TODO We might want to "merge" attribute sets with
                                        // the same OID - currently, differing values give an error
                                        if (attributes.Contains(aOid.Id))
                                        {
                                            // OK, but the value has to be the same
                                            if (!attributes[aOid.Id].Equals(attr))
                                            {
                                                throw new IOException("attempt to add existing attribute with different value");
                                            }
                                        }
                                        else
                                        {
                                            attributes.Add(aOid.Id, attr);
                                        }

                                        if (aOid.Equals(PkcsObjectIdentifiers.Pkcs9AtFriendlyName))
                                        {
                                            alias = ((DerBmpString) attr).GetString();
                                            // TODO Do these in a separate loop, just collect aliases here
                                            _keys[alias] = pkcs12Key;
                                        }
                                        else if (aOid.Equals(PkcsObjectIdentifiers.Pkcs9AtLocalKeyID))
                                        {
                                            localId = (Asn1OctetString) attr;
                                        }
                                    }
                                }

                                // TODO Should we be checking localIds != null here
                                // as for PkcsObjectIdentifiers.Data version above?

                                string name = Hex.ToHexString(localId.GetOctets());

                                if (alias == null)
                                {
                                    _keys[name] = pkcs12Key;
                                }
                                else
                                {
                                    // TODO There may have been more than one alias
                                    _localIds[alias] = name;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("extra " + b.BagID);
                                Debug.WriteLine("extra " + Asn1Dump.DumpAsString(b));
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("extra " + oid);
                        Debug.WriteLine("extra " + Asn1Dump.DumpAsString(ci.Content));
                    }
                }
            }

            _certs.Clear();
            _chainCerts.Clear();
            _keyCerts.Clear();

            foreach (SafeBag b in chain)
            {
                var cb = new CertBag((Asn1Sequence) b.BagValue);
                byte[] octets = ((Asn1OctetString) cb.CertValue).GetOctets();
                X509Certificate cert = new X509CertificateParser().ReadCertificate(octets);

                //
                // set the attributes
                //
                IDictionary attributes = Platform.CreateHashtable();
                Asn1OctetString localId = null;
                string alias = null;

                if (b.BagAttributes != null)
                {
                    foreach (Asn1Sequence sq in b.BagAttributes)
                    {
                        var aOid = (DerObjectIdentifier) sq[0];
                        var attrSet = (Asn1Set) sq[1];

                        if (attrSet.Count > 0)
                        {
                            // TODO We should be adding all attributes in the set
                            Asn1Encodable attr = attrSet[0];

                            // TODO We might want to "merge" attribute sets with
                            // the same OID - currently, differing values give an error
                            if (attributes.Contains(aOid.Id))
                            {
                                // OK, but the value has to be the same
                                if (!attributes[aOid.Id].Equals(attr))
                                {
                                    throw new IOException("attempt to add existing attribute with different value");
                                }
                            }
                            else
                            {
                                attributes.Add(aOid.Id, attr);
                            }

                            if (aOid.Equals(PkcsObjectIdentifiers.Pkcs9AtFriendlyName))
                            {
                                alias = ((DerBmpString) attr).GetString();
                            }
                            else if (aOid.Equals(PkcsObjectIdentifiers.Pkcs9AtLocalKeyID))
                            {
                                localId = (Asn1OctetString) attr;
                            }
                        }
                    }
                }

                var certId = new CertId(cert.GetPublicKey());
                var pkcs12Cert = new X509CertificateEntry(cert, attributes);

                _chainCerts[certId] = pkcs12Cert;

                if (unmarkedKey)
                {
                    if (_keyCerts.Count == 0)
                    {
                        string name = Hex.ToHexString(certId.Id);

                        _keyCerts[name] = pkcs12Cert;

                        object temp = _keys["unmarked"];
                        _keys.Remove("unmarked");
                        _keys[name] = temp;
                    }
                }
                else
                {
                    if (localId != null)
                    {
                        string name = Hex.ToHexString(localId.GetOctets());

                        _keyCerts[name] = pkcs12Cert;
                    }

                    if (alias != null)
                    {
                        // TODO There may have been more than one alias
                        _certs[alias] = pkcs12Cert;
                    }
                }
            }
        }

        public AsymmetricKeyEntry GetKey(string alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException("alias");
            }

            return (AsymmetricKeyEntry) _keys[alias];
        }

        public bool IsCertificateEntry(string alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException("alias");
            }

            return (_certs[alias] != null && _keys[alias] == null);
        }

        public bool IsKeyEntry(string alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException("alias");
            }

            return (_keys[alias] != null);
        }

        private IDictionary GetAliasesTable()
        {
            IDictionary tab = Platform.CreateHashtable();

            foreach (string key in _certs.Keys)
            {
                tab[key] = "cert";
            }

            foreach (string a in _keys.Keys)
            {
                if (tab[a] == null)
                {
                    tab[a] = "key";
                }
            }

            return tab;
        }

        public bool ContainsAlias(string alias)
        {
            return _certs[alias] != null || _keys[alias] != null;
        }

        /**
         * simply return the cert entry for the private key
         */

        public X509CertificateEntry GetCertificate(string alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException("alias");
            }

            var c = (X509CertificateEntry) _certs[alias];

            //
            // look up the key table - and try the local key id
            //
            if (c == null)
            {
                var id = (string) _localIds[alias];
                if (id != null)
                {
                    c = (X509CertificateEntry) _keyCerts[id];
                }
                else
                {
                    c = (X509CertificateEntry) _keyCerts[alias];
                }
            }

            return c;
        }

        public string GetCertificateAlias(X509Certificate cert)
        {
            if (cert == null)
            {
                throw new ArgumentNullException("cert");
            }

            foreach (DictionaryEntry entry in _certs)
            {
                var entryValue = (X509CertificateEntry) entry.Value;
                if (entryValue.Certificate.Equals(cert))
                {
                    return (string) entry.Key;
                }
            }

            foreach (DictionaryEntry entry in _keyCerts)
            {
                var entryValue = (X509CertificateEntry) entry.Value;
                if (entryValue.Certificate.Equals(cert))
                {
                    return (string) entry.Key;
                }
            }

            return null;
        }

        public X509CertificateEntry[] GetCertificateChain(string alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException("alias");
            }

            if (!IsKeyEntry(alias))
            {
                return null;
            }

            X509CertificateEntry c = GetCertificate(alias);

            if (c != null)
            {
                IList cs = Platform.CreateArrayList();

                while (c != null)
                {
                    X509Certificate x509c = c.Certificate;
                    X509CertificateEntry nextC = null;

                    Asn1OctetString ext = x509c.GetExtensionValue(X509Extensions.AuthorityKeyIdentifier);
                    if (ext != null)
                    {
                        AuthorityKeyIdentifier id = AuthorityKeyIdentifier.GetInstance(Asn1Object.FromByteArray(ext.GetOctets()));

                        if (id.GetKeyIdentifier() != null)
                        {
                            nextC = (X509CertificateEntry) _chainCerts[new CertId(id.GetKeyIdentifier())];
                        }
                    }

                    if (nextC == null)
                    {
                        //
                        // no authority key id, try the Issuer DN
                        //
                        X509Name i = x509c.IssuerDN;
                        X509Name s = x509c.SubjectDN;

                        if (!i.Equivalent(s))
                        {
                            foreach (CertId certId in _chainCerts.Keys)
                            {
                                var x509CertEntry = (X509CertificateEntry) _chainCerts[certId];

                                X509Certificate crt = x509CertEntry.Certificate;

                                X509Name sub = crt.SubjectDN;
                                if (sub.Equivalent(i))
                                {
                                    try
                                    {
                                        x509c.Verify(crt.GetPublicKey());

                                        nextC = x509CertEntry;
                                        break;
                                    }
                                    catch (InvalidKeyException)
                                    {
                                        // TODO What if it doesn't verify?
                                    }
                                }
                            }
                        }
                    }

                    cs.Add(c);
                    if (nextC != c) // self signed - end of the chain
                    {
                        c = nextC;
                    }
                    else
                    {
                        c = null;
                    }
                }

                var result = new X509CertificateEntry[cs.Count];
                for (int i = 0; i < cs.Count; ++i)
                {
                    result[i] = (X509CertificateEntry) cs[i];
                }
                return result;
            }

            return null;
        }

        public void SetCertificateEntry(string alias, X509CertificateEntry certEntry)
        {
            if (alias == null)
            {
                throw new ArgumentNullException("alias");
            }
            if (certEntry == null)
            {
                throw new ArgumentNullException("certEntry");
            }
            if (_keys[alias] != null)
            {
                throw new ArgumentException("There is a key entry with the name " + alias + ".");
            }

            _certs[alias] = certEntry;
            _chainCerts[new CertId(certEntry.Certificate.GetPublicKey())] = certEntry;
        }

        public void SetKeyEntry(string alias, AsymmetricKeyEntry keyEntry, X509CertificateEntry[] chain)
        {
            if (alias == null)
            {
                throw new ArgumentNullException("alias");
            }
            if (keyEntry == null)
            {
                throw new ArgumentNullException("keyEntry");
            }
            if (keyEntry.Key.IsPrivate && (chain == null))
            {
                throw new ArgumentException("No certificate chain for private key");
            }

            if (_keys[alias] != null)
            {
                DeleteEntry(alias);
            }

            _keys[alias] = keyEntry;
            _certs[alias] = chain[0];

            for (int i = 0; i != chain.Length; i++)
            {
                _chainCerts[new CertId(chain[i].Certificate.GetPublicKey())] = chain[i];
            }
        }

        public void DeleteEntry(string alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException("alias");
            }

            var k = (AsymmetricKeyEntry) _keys[alias];
            if (k != null)
            {
                _keys.Remove(alias);
            }

            var c = (X509CertificateEntry) _certs[alias];

            if (c != null)
            {
                _certs.Remove(alias);
                _chainCerts.Remove(new CertId(c.Certificate.GetPublicKey()));
            }

            if (k != null)
            {
                var id = (string) _localIds[alias];
                if (id != null)
                {
                    _localIds.Remove(alias);
                    c = (X509CertificateEntry) _keyCerts[id];
                }
                if (c != null)
                {
                    _keyCerts.Remove(id);
                    _chainCerts.Remove(new CertId(c.Certificate.GetPublicKey()));
                }
            }

            if (c == null && k == null)
            {
                throw new ArgumentException("no such entry as " + alias);
            }
        }

        public bool IsEntryOfType(string alias, Type entryType)
        {
            if (entryType == typeof (X509CertificateEntry))
            {
                return IsCertificateEntry(alias);
            }

            if (entryType == typeof (AsymmetricKeyEntry))
            {
                return IsKeyEntry(alias) && GetCertificate(alias) != null;
            }

            return false;
        }

        [Obsolete("Use 'Count' property instead")]
        public int Size()
        {
            return Count;
        }

        public void Save(Stream stream, char[] password, SecureRandom random)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            if (random == null)
            {
                throw new ArgumentNullException("random");
            }

            //
            // handle the key
            //
            var keyS = new Asn1EncodableVector();
            foreach (string name in _keys.Keys)
            {
                var kSalt = new byte[SaltSize];
                random.NextBytes(kSalt);

                var privKey = (AsymmetricKeyEntry) _keys[name];
                EncryptedPrivateKeyInfo kInfo = EncryptedPrivateKeyInfoFactory.CreateEncryptedPrivateKeyInfo(_keyAlgorithm, password, kSalt, MinIterations, privKey.Key);

                var kName = new Asn1EncodableVector();

                foreach (string oid in privKey.BagAttributeKeys)
                {
                    Asn1Encodable entry = privKey[oid];

                    // NB: Ignore any existing FriendlyName
                    if (oid.Equals(PkcsObjectIdentifiers.Pkcs9AtFriendlyName.Id))
                    {
                        continue;
                    }

                    kName.Add(new DerSequence(new DerObjectIdentifier(oid), new DerSet(entry)));
                }

                //
                // make sure we are using the local alias on store
                //
                // NB: We always set the FriendlyName based on 'name'
                //if (privKey[PkcsObjectIdentifiers.Pkcs9AtFriendlyName] == null)
                {
                    kName.Add(new DerSequence(PkcsObjectIdentifiers.Pkcs9AtFriendlyName, new DerSet(new DerBmpString(name))));
                }

                //
                // make sure we have a local key-id
                //
                if (privKey[PkcsObjectIdentifiers.Pkcs9AtLocalKeyID] == null)
                {
                    X509CertificateEntry ct = GetCertificate(name);
                    AsymmetricKeyParameter pubKey = ct.Certificate.GetPublicKey();
                    SubjectKeyIdentifier subjectKeyID = CreateSubjectKeyID(pubKey);

                    kName.Add(new DerSequence(PkcsObjectIdentifiers.Pkcs9AtLocalKeyID, new DerSet(subjectKeyID)));
                }

                var kBag = new SafeBag(PkcsObjectIdentifiers.Pkcs8ShroudedKeyBag, kInfo.ToAsn1Object(), new DerSet(kName));
                keyS.Add(kBag);
            }

            byte[] derEncodedBytes = new DerSequence(keyS).GetDerEncoded();

            var keyString = new BerOctetString(derEncodedBytes);

            //
            // certificate processing
            //
            var cSalt = new byte[SaltSize];

            random.NextBytes(cSalt);

            var certSeq = new Asn1EncodableVector();
            var cParams = new Pkcs12PbeParams(cSalt, MinIterations);
            var cAlgId = new AlgorithmIdentifier(_certAlgorithm, cParams.ToAsn1Object());
            ISet doneCerts = new HashSet();

            foreach (string name in _keys.Keys)
            {
                X509CertificateEntry certEntry = GetCertificate(name);
                var cBag = new CertBag(PkcsObjectIdentifiers.X509Certificate, new DerOctetString(certEntry.Certificate.GetEncoded()));

                var fName = new Asn1EncodableVector();

                foreach (string oid in certEntry.BagAttributeKeys)
                {
                    Asn1Encodable entry = certEntry[oid];

                    // NB: Ignore any existing FriendlyName
                    if (oid.Equals(PkcsObjectIdentifiers.Pkcs9AtFriendlyName.Id))
                    {
                        continue;
                    }

                    fName.Add(new DerSequence(new DerObjectIdentifier(oid), new DerSet(entry)));
                }

                //
                // make sure we are using the local alias on store
                //
                // NB: We always set the FriendlyName based on 'name'
                //if (certEntry[PkcsObjectIdentifiers.Pkcs9AtFriendlyName] == null)
                {
                    fName.Add(new DerSequence(PkcsObjectIdentifiers.Pkcs9AtFriendlyName, new DerSet(new DerBmpString(name))));
                }

                //
                // make sure we have a local key-id
                //
                if (certEntry[PkcsObjectIdentifiers.Pkcs9AtLocalKeyID] == null)
                {
                    AsymmetricKeyParameter pubKey = certEntry.Certificate.GetPublicKey();
                    SubjectKeyIdentifier subjectKeyID = CreateSubjectKeyID(pubKey);

                    fName.Add(new DerSequence(PkcsObjectIdentifiers.Pkcs9AtLocalKeyID, new DerSet(subjectKeyID)));
                }

                var sBag = new SafeBag(PkcsObjectIdentifiers.CertBag, cBag.ToAsn1Object(), new DerSet(fName));

                certSeq.Add(sBag);

                doneCerts.Add(certEntry.Certificate);
            }

            foreach (string certId in _certs.Keys)
            {
                var cert = (X509CertificateEntry) _certs[certId];

                if (_keys[certId] != null)
                {
                    continue;
                }

                var cBag = new CertBag(PkcsObjectIdentifiers.X509Certificate, new DerOctetString(cert.Certificate.GetEncoded()));

                var fName = new Asn1EncodableVector();

                foreach (string oid in cert.BagAttributeKeys)
                {
                    // a certificate not immediately linked to a key doesn't require
                    // a localKeyID and will confuse some PKCS12 implementations.
                    //
                    // If we find one, we'll prune it out.
                    if (oid.Equals(PkcsObjectIdentifiers.Pkcs9AtLocalKeyID.Id))
                    {
                        continue;
                    }

                    Asn1Encodable entry = cert[oid];

                    // NB: Ignore any existing FriendlyName
                    if (oid.Equals(PkcsObjectIdentifiers.Pkcs9AtFriendlyName.Id))
                    {
                        continue;
                    }

                    fName.Add(new DerSequence(new DerObjectIdentifier(oid), new DerSet(entry)));
                }

                //
                // make sure we are using the local alias on store
                //
                // NB: We always set the FriendlyName based on 'certId'
                //if (cert[PkcsObjectIdentifiers.Pkcs9AtFriendlyName] == null)
                {
                    fName.Add(new DerSequence(PkcsObjectIdentifiers.Pkcs9AtFriendlyName, new DerSet(new DerBmpString(certId))));
                }

                var sBag = new SafeBag(PkcsObjectIdentifiers.CertBag, cBag.ToAsn1Object(), new DerSet(fName));

                certSeq.Add(sBag);

                doneCerts.Add(cert.Certificate);
            }

            foreach (CertId certId in _chainCerts.Keys)
            {
                var cert = (X509CertificateEntry) _chainCerts[certId];

                if (doneCerts.Contains(cert.Certificate))
                {
                    continue;
                }

                var cBag = new CertBag(PkcsObjectIdentifiers.X509Certificate, new DerOctetString(cert.Certificate.GetEncoded()));

                var fName = new Asn1EncodableVector();

                foreach (string oid in cert.BagAttributeKeys)
                {
                    // a certificate not immediately linked to a key doesn't require
                    // a localKeyID and will confuse some PKCS12 implementations.
                    //
                    // If we find one, we'll prune it out.
                    if (oid.Equals(PkcsObjectIdentifiers.Pkcs9AtLocalKeyID.Id))
                    {
                        continue;
                    }

                    fName.Add(new DerSequence(new DerObjectIdentifier(oid), new DerSet(cert[oid])));
                }

                var sBag = new SafeBag(PkcsObjectIdentifiers.CertBag, cBag.ToAsn1Object(), new DerSet(fName));

                certSeq.Add(sBag);
            }

            derEncodedBytes = new DerSequence(certSeq).GetDerEncoded();

            byte[] certBytes = CryptPbeData(true, cAlgId, password, false, derEncodedBytes);

            var cInfo = new EncryptedData(PkcsObjectIdentifiers.Data, cAlgId, new BerOctetString(certBytes));

            ContentInfo[] info = {new ContentInfo(PkcsObjectIdentifiers.Data, keyString), new ContentInfo(PkcsObjectIdentifiers.EncryptedData, cInfo.ToAsn1Object())};

            byte[] data = new AuthenticatedSafe(info).GetEncoded(_useDerEncoding ? Asn1Encodable.Der : Asn1Encodable.Ber);

            var mainInfo = new ContentInfo(PkcsObjectIdentifiers.Data, new BerOctetString(data));

            //
            // create the mac
            //
            var mSalt = new byte[20];
            random.NextBytes(mSalt);

            byte[] mac = CalculatePbeMac(OiwObjectIdentifiers.IdSha1, mSalt, MinIterations, password, false, data);

            var algId = new AlgorithmIdentifier(OiwObjectIdentifiers.IdSha1, DerNull.Instance);
            var dInfo = new DigestInfo(algId, mac);

            var mData = new MacData(dInfo, mSalt, MinIterations);

            //
            // output the Pfx
            //
            var pfx = new Pfx(mainInfo, mData);

            DerOutputStream derOut;
            if (_useDerEncoding)
            {
                derOut = new DerOutputStream(stream);
            }
            else
            {
                derOut = new BerOutputStream(stream);
            }

            derOut.WriteObject(pfx);
        }

        internal static byte[] CalculatePbeMac(DerObjectIdentifier oid, byte[] salt, int itCount, char[] password, bool wrongPkcs12Zero, byte[] data)
        {
            Asn1Encodable asn1Params = PbeUtilities.GenerateAlgorithmParameters(oid, salt, itCount);
            ICipherParameters cipherParams = PbeUtilities.GenerateCipherParameters(oid, password, wrongPkcs12Zero, asn1Params);

            var mac = (IMac) PbeUtilities.CreateEngine(oid);
            mac.Init(cipherParams);
            mac.BlockUpdate(data, 0, data.Length);
            return MacUtilities.DoFinal(mac);
        }

        private static byte[] CryptPbeData(bool forEncryption, AlgorithmIdentifier algId, char[] password, bool wrongPkcs12Zero, byte[] data)
        {
            Pkcs12PbeParams pbeParams = Pkcs12PbeParams.GetInstance(algId.Parameters);
            ICipherParameters cipherParams = PbeUtilities.GenerateCipherParameters(algId.ObjectID, password, wrongPkcs12Zero, pbeParams);

            var cipher = PbeUtilities.CreateEngine(algId.ObjectID) as IBufferedCipher;

            if (cipher == null)
            {
                throw new Exception("Unknown encryption algorithm: " + algId.ObjectID);
            }

            cipher.Init(forEncryption, cipherParams);

            return cipher.DoFinal(data);
        }

        internal class CertId
        {
            private readonly byte[] id;

            internal CertId(AsymmetricKeyParameter pubKey)
            {
                id = CreateSubjectKeyID(pubKey).GetKeyIdentifier();
            }

            internal CertId(byte[] id)
            {
                this.id = id;
            }

            internal byte[] Id
            {
                get { return id; }
            }

            public override int GetHashCode()
            {
                return Arrays.GetHashCode(id);
            }

            public override bool Equals(object obj)
            {
                if (obj == this)
                {
                    return true;
                }

                var other = obj as CertId;

                if (other == null)
                {
                    return false;
                }

                return Arrays.AreEqual(id, other.id);
            }
        }

        private class IgnoresCaseHashtable : IEnumerable
        {
            private readonly IDictionary keys = Platform.CreateHashtable();
            private readonly IDictionary orig = Platform.CreateHashtable();

            public ICollection Keys
            {
                get { return orig.Keys; }
            }

            public object this[string alias]
            {
                get
                {
                    string lower = alias.ToLowerInvariant();
                    var k = (string) keys[lower];

                    if (k == null)
                    {
                        return null;
                    }

                    return orig[k];
                }
                set
                {
                    string lower = alias.ToLowerInvariant();
                    var k = (string) keys[lower];
                    if (k != null)
                    {
                        orig.Remove(k);
                    }
                    keys[lower] = alias;
                    orig[alias] = value;
                }
            }

            public ICollection Values
            {
                get { return orig.Values; }
            }

            public IEnumerator GetEnumerator()
            {
                return orig.GetEnumerator();
            }

            public void Clear()
            {
                orig.Clear();
                keys.Clear();
            }

            public object Remove(string alias)
            {
                string lower = alias.ToLowerInvariant();
                var k = (string) keys[lower];

                if (k == null)
                {
                    return null;
                }

                keys.Remove(lower);

                object o = orig[k];
                orig.Remove(k);
                return o;
            }
        }
    }
}
