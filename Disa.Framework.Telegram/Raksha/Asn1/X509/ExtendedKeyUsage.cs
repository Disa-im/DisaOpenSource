// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtendedKeyUsage.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using Raksha.Utilities;

namespace Raksha.Asn1.X509
{
    /// <summary>
    ///     The extendedKeyUsage object.
    /// </summary>
    /// <code>
    ///     extendedKeyUsage ::= Sequence SIZE (1..MAX) OF KeyPurposeId
    /// </code>
    public class ExtendedKeyUsage : Asn1Encodable
    {
        private readonly Asn1Sequence _seq;
        private readonly IDictionary _usageTable = Platform.CreateHashtable();

        private ExtendedKeyUsage(Asn1Sequence seq)
        {
            this._seq = seq;

            foreach (object o in seq)
            {
                if (!(o is DerObjectIdentifier))
                {
                    throw new ArgumentException("Only DerObjectIdentifier instances allowed in ExtendedKeyUsage.");
                }

                _usageTable.Add(o, o);
            }
        }

        public ExtendedKeyUsage(params KeyPurposeID[] usages)
        {
            _seq = new DerSequence(usages);

            foreach (KeyPurposeID usage in usages)
            {
                _usageTable.Add(usage, usage);
            }
        }

        public ExtendedKeyUsage(IEnumerable usages)
        {
            var v = new Asn1EncodableVector();

            foreach (Asn1Object o in usages)
            {
                v.Add(o);

                _usageTable.Add(o, o);
            }

            _seq = new DerSequence(v);
        }

        public int Count
        {
            get { return _usageTable.Count; }
        }

        public static ExtendedKeyUsage GetInstance(Asn1TaggedObject obj, bool explicitly)
        {
            return GetInstance(Asn1Sequence.GetInstance(obj, explicitly));
        }

        public static ExtendedKeyUsage GetInstance(object obj)
        {
            if (obj is ExtendedKeyUsage)
            {
                return (ExtendedKeyUsage) obj;
            }

            if (obj is Asn1Sequence)
            {
                return new ExtendedKeyUsage((Asn1Sequence) obj);
            }

            if (obj is X509Extension)
            {
                return GetInstance(X509Extension.ConvertValueToObject((X509Extension) obj));
            }

            throw new ArgumentException("Invalid ExtendedKeyUsage: " + obj.GetType().Name);
        }

        public bool HasKeyPurposeId(KeyPurposeID keyPurposeId)
        {
            return _usageTable[keyPurposeId] != null;
        }

        /**
		 * Returns all extended key usages.
		 * The returned ArrayList contains DerObjectIdentifier instances.
		 * @return An ArrayList with all key purposes.
		 */

        public IList GetAllUsages()
        {
            return Platform.CreateArrayList(_usageTable.Values);
        }

        public override Asn1Object ToAsn1Object()
        {
            return _seq;
        }
    }
}
