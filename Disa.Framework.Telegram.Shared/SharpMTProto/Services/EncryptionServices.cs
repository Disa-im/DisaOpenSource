// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EncryptionServices.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using BigMath;
using Raksha.Crypto.Engines;
using Raksha.Crypto.Parameters;
using SharpMTProto.Authentication;

namespace SharpMTProto.Services
{
    public partial class EncryptionServices : IEncryptionServices
    {
        public byte[] RSAEncrypt(byte[] data, PublicKey publicKey)
        {
            var rsa = new RsaEngine();
            var modulus = new BigInteger(publicKey.Modulus);
            var exponent = new BigInteger(publicKey.Exponent);
            rsa.Init(true, new RsaKeyParameters(false, modulus, exponent));
            return rsa.ProcessBlock(data, 0, data.Length);
        }

        public DHOutParams DH(byte[] b, byte[] g, byte[] ga, byte[] p)
        {
            var bi = new BigInteger(1, b);
            var gi = new BigInteger(1, g);
            var gai = new BigInteger(1, ga);
            var pi = new BigInteger(1, p);

            byte[] gb = gi.ModPow(bi, pi).ToByteArray();
            byte[] s = gai.ModPow(bi, pi).ToByteArray();

            return new DHOutParams(gb, s);
        }

        private static byte[] Xor(byte[] buffer1, byte[] buffer2)
        {
            var result = new byte[buffer1.Length];
            for (int i = 0; i < buffer1.Length; i++)
            {
                result[i] = (byte) (buffer1[i] ^ buffer2[i]);
            }
            return result;
        }
    }
}
