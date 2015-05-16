// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NISTNamedCurves.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using Raksha.Asn1.Sec;
using Raksha.Asn1.X9;
using Raksha.Utilities;
using Raksha.Utilities.Collections;

namespace Raksha.Asn1.Nist
{
    /// <summary>
    ///     Utility class for fetching curves using their NIST names as published in FIPS-PUB 186-2.
    /// </summary>
    public static class NistNamedCurves
    {
        private static readonly IDictionary ObjIds = Platform.CreateHashtable();
        private static readonly IDictionary NamesPrivate = Platform.CreateHashtable();

        static NistNamedCurves()
        {
            DefineCurve("B-571", SecObjectIdentifiers.SecT571r1);
            DefineCurve("B-409", SecObjectIdentifiers.SecT409r1);
            DefineCurve("B-283", SecObjectIdentifiers.SecT283r1);
            DefineCurve("B-233", SecObjectIdentifiers.SecT233r1);
            DefineCurve("B-163", SecObjectIdentifiers.SecT163r2);
            DefineCurve("P-521", SecObjectIdentifiers.SecP521r1);
            DefineCurve("P-384", SecObjectIdentifiers.SecP384r1);
            DefineCurve("P-256", SecObjectIdentifiers.SecP256r1);
            DefineCurve("P-224", SecObjectIdentifiers.SecP224r1);
            DefineCurve("P-192", SecObjectIdentifiers.SecP192r1);
        }

        public static IEnumerable Names
        {
            get { return new EnumerableProxy(ObjIds.Keys); }
        }

        private static void DefineCurve(string name, DerObjectIdentifier oid)
        {
            ObjIds.Add(name, oid);
            NamesPrivate.Add(oid, name);
        }

        public static X9ECParameters GetByName(string name)
        {
            var oid = (DerObjectIdentifier) ObjIds[name.ToUpperInvariant()];

            if (oid != null)
            {
                return GetByOid(oid);
            }

            return null;
        }

        /// <summary>
        ///     Get by object identifier.
        /// </summary>
        /// <param name="oid">An object identifier representing a named curve, if present.</param>
        /// <returns>
        ///     The X9ECParameters object for the named curve represented by the passed in object identifier.
        ///     Null if the curve isn't present.
        /// </returns>
        public static X9ECParameters GetByOid(DerObjectIdentifier oid)
        {
            return SecNamedCurves.GetByOid(oid);
        }

        /// <summary>
        ///     Get the object identifier.
        /// </summary>
        /// <param name="name">Object name.</param>
        /// <returns>
        ///     The object identifier signified by the passed in name.
        ///     Null if there is no object identifier associated with name.
        /// </returns>
        public static DerObjectIdentifier GetOid(string name)
        {
            return (DerObjectIdentifier) ObjIds[name.ToUpperInvariant()];
        }

        /// <summary>
        ///     Get the named curve name represented by the given object identifier.
        /// </summary>
        /// <param name="oid">Object identifier.</param>
        public static string GetName(DerObjectIdentifier oid)
        {
            return (string) NamesPrivate[oid];
        }
    }
}
