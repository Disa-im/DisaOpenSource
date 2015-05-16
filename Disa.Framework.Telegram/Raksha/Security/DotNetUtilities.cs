// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DotNetUtilities.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using BigMath;
using Raksha.Asn1.X509;
using Raksha.Crypto;
using Raksha.Crypto.Parameters;
using Raksha.X509;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace Raksha.Security
{
    /// <summary>
    ///     A class containing methods to interface the BouncyCastle world to the .NET Crypto world.
    /// </summary>
    public static class DotNetUtilities
    {
        /// <summary>
        ///     Create an System.Security.Cryptography.X509Certificate from an X509Certificate Structure.
        /// </summary>
        /// <param name="x509Struct"></param>
        /// <returns>A System.Security.Cryptography.X509Certificate.</returns>
        public static X509Certificate ToX509Certificate(X509CertificateStructure x509Struct)
        {
            return new X509Certificate(x509Struct.GetDerEncoded());
        }

        public static X509Certificate ToX509Certificate(X509.X509Certificate x509Cert)
        {
            return new X509Certificate(x509Cert.GetEncoded());
        }

        public static X509.X509Certificate FromX509Certificate(X509Certificate x509Cert)
        {
            return new X509CertificateParser().ReadCertificate(x509Cert.GetRawCertData());
        }

        public static AsymmetricCipherKeyPair GetDsaKeyPair(DSA dsa)
        {
            return GetDsaKeyPair(dsa.ExportParameters(true));
        }

        public static AsymmetricCipherKeyPair GetDsaKeyPair(DSAParameters dp)
        {
            DsaValidationParameters validationParameters = (dp.Seed != null) ? new DsaValidationParameters(dp.Seed, dp.Counter) : null;

            var parameters = new DsaParameters(new BigInteger(1, dp.P), new BigInteger(1, dp.Q), new BigInteger(1, dp.G), validationParameters);

            var pubKey = new DsaPublicKeyParameters(new BigInteger(1, dp.Y), parameters);

            var privKey = new DsaPrivateKeyParameters(new BigInteger(1, dp.X), parameters);

            return new AsymmetricCipherKeyPair(pubKey, privKey);
        }

        public static DsaPublicKeyParameters GetDsaPublicKey(DSA dsa)
        {
            return GetDsaPublicKey(dsa.ExportParameters(false));
        }

        public static DsaPublicKeyParameters GetDsaPublicKey(DSAParameters dp)
        {
            DsaValidationParameters validationParameters = (dp.Seed != null) ? new DsaValidationParameters(dp.Seed, dp.Counter) : null;

            var parameters = new DsaParameters(new BigInteger(1, dp.P), new BigInteger(1, dp.Q), new BigInteger(1, dp.G), validationParameters);

            return new DsaPublicKeyParameters(new BigInteger(1, dp.Y), parameters);
        }

        public static AsymmetricCipherKeyPair GetRsaKeyPair(RSA rsa)
        {
            return GetRsaKeyPair(rsa.ExportParameters(true));
        }

        public static AsymmetricCipherKeyPair GetRsaKeyPair(RSAParameters rp)
        {
            var modulus = new BigInteger(1, rp.Modulus);
            var pubExp = new BigInteger(1, rp.Exponent);

            var pubKey = new RsaKeyParameters(false, modulus, pubExp);

            var privKey = new RsaPrivateCrtKeyParameters(modulus, pubExp, new BigInteger(1, rp.D), new BigInteger(1, rp.P), new BigInteger(1, rp.Q),
                new BigInteger(1, rp.DP), new BigInteger(1, rp.DQ), new BigInteger(1, rp.InverseQ));

            return new AsymmetricCipherKeyPair(pubKey, privKey);
        }

        public static RsaKeyParameters GetRsaPublicKey(RSA rsa)
        {
            return GetRsaPublicKey(rsa.ExportParameters(false));
        }

        public static RsaKeyParameters GetRsaPublicKey(RSAParameters rp)
        {
            return new RsaKeyParameters(false, new BigInteger(1, rp.Modulus), new BigInteger(1, rp.Exponent));
        }

        public static AsymmetricCipherKeyPair GetKeyPair(AsymmetricAlgorithm privateKey)
        {
            if (privateKey is DSA)
            {
                return GetDsaKeyPair((DSA) privateKey);
            }

            if (privateKey is RSA)
            {
                return GetRsaKeyPair((RSA) privateKey);
            }

            throw new ArgumentException("Unsupported algorithm specified", "privateKey");
        }

        public static RSA ToRSA(RsaKeyParameters rsaKey)
        {
            RSAParameters rp = ToRSAParameters(rsaKey);
            var rsaCsp = new RSACryptoServiceProvider();
            rsaCsp.ImportParameters(rp);
            return rsaCsp;
        }

        public static RSA ToRSA(RsaPrivateCrtKeyParameters privKey)
        {
            RSAParameters rp = ToRSAParameters(privKey);
            var rsaCsp = new RSACryptoServiceProvider();
            rsaCsp.ImportParameters(rp);
            return rsaCsp;
        }

        public static RSAParameters ToRSAParameters(RsaKeyParameters rsaKey)
        {
            var rp = new RSAParameters();
            rp.Modulus = rsaKey.Modulus.ToByteArrayUnsigned();
            if (rsaKey.IsPrivate)
            {
                rp.D = rsaKey.Exponent.ToByteArrayUnsigned();
            }
            else
            {
                rp.Exponent = rsaKey.Exponent.ToByteArrayUnsigned();
            }
            return rp;
        }

        public static RSAParameters ToRSAParameters(RsaPrivateCrtKeyParameters privKey)
        {
            var rp = new RSAParameters
            {
                Modulus = privKey.Modulus.ToByteArrayUnsigned(),
                Exponent = privKey.PublicExponent.ToByteArrayUnsigned(),
                D = privKey.Exponent.ToByteArrayUnsigned(),
                P = privKey.P.ToByteArrayUnsigned(),
                Q = privKey.Q.ToByteArrayUnsigned(),
                DP = privKey.DP.ToByteArrayUnsigned(),
                DQ = privKey.DQ.ToByteArrayUnsigned(),
                InverseQ = privKey.QInv.ToByteArrayUnsigned()
            };
            return rp;
        }
    }
}
