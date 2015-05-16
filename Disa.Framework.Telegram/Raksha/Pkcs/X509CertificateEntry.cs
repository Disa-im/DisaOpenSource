// --------------------------------------------------------------------------------------------------------------------
// <copyright file="X509CertificateEntry.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using Raksha.Utilities;
using Raksha.X509;

namespace Raksha.Pkcs
{
    public class X509CertificateEntry : Pkcs12Entry
    {
        private readonly X509Certificate _cert;

        public X509CertificateEntry(X509Certificate cert) : base(Platform.CreateHashtable())
        {
            _cert = cert;
        }

        public X509CertificateEntry(X509Certificate cert, IDictionary attributes) : base(attributes)
        {
            _cert = cert;
        }

        public X509Certificate Certificate
        {
            get { return _cert; }
        }

        public override bool Equals(object obj)
        {
            var other = obj as X509CertificateEntry;

            if (other == null)
            {
                return false;
            }

            return _cert.Equals(other._cert);
        }

        public override int GetHashCode()
        {
            return ~_cert.GetHashCode();
        }
    }
}
