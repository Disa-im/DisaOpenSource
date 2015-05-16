// --------------------------------------------------------------------------------------------------------------------
// <copyright file="X509StoreFactory.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Reflection;

namespace Raksha.X509.Store
{
    public static class X509StoreFactory
    {
        public static IX509Store Create(string type, IX509StoreParameters parameters)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            string[] parts = type.ToUpperInvariant().Split('/');

            if (parts.Length < 2)
            {
                throw new ArgumentException("type");
            }

            if (parts[1] != "COLLECTION")
            {
                throw new NoSuchStoreException("X.509 store type '" + type + "' not available.");
            }

            var p = (X509CollectionStoreParameters) parameters;
            ICollection coll = p.GetCollection();

            switch (parts[0])
            {
                case "ATTRIBUTECERTIFICATE":
                    CheckCorrectType(coll, typeof (IX509AttributeCertificate));
                    break;
                case "CERTIFICATE":
                    CheckCorrectType(coll, typeof (X509Certificate));
                    break;
                case "CERTIFICATEPAIR":
                    CheckCorrectType(coll, typeof (X509CertificatePair));
                    break;
                case "CRL":
                    CheckCorrectType(coll, typeof (X509Crl));
                    break;
                default:
                    throw new NoSuchStoreException("X.509 store type '" + type + "' not available.");
            }

            return new X509CollectionStore(coll);
        }

        private static void CheckCorrectType(IEnumerable coll, Type t)
        {
            foreach (object o in coll)
            {
                if (!t.GetTypeInfo().IsAssignableFrom(o.GetType().GetTypeInfo()))
                {
                    throw new InvalidCastException("Can't cast object to type: " + t.FullName);
                }
            }
        }
    }
}
