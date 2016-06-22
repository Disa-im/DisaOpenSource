// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PublicKey.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using BigMath.Utils;
using SharpTL;

namespace SharpMTProto.Authentication
{
    /// <summary>
    ///     Contains public key, exponent and fingerprint.
    /// </summary>
    [TLObject(0xEED4C70BU)]
    public class PublicKey
    {
        public PublicKey()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PublicKey" /> class.
        /// </summary>
        /// <param name="modulus">Modulus as a HEX string.</param>
        /// <param name="exponent">Exponent as a HEX string.</param>
        /// <param name="fingerprint">Fingerprint.</param>
        public PublicKey(string modulus, string exponent, ulong fingerprint) : this(modulus.HexToBytes(), exponent.HexToBytes(), fingerprint)
        {
        }

        public PublicKey(byte[] modulus, byte[] exponent, ulong fingerprint)
        {
            Modulus = modulus;
            Exponent = exponent;
            Fingerprint = fingerprint;
        }

        [TLProperty(1)]
        public byte[] Modulus { get; set; }

        [TLProperty(2)]
        public byte[] Exponent { get; set; }

        /// <summary>
        ///     Represents lower 64 bits of the SHA1(Modulus).
        /// </summary>
        public ulong Fingerprint { get; set; }
    }
}
