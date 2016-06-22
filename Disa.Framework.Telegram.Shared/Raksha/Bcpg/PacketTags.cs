// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PacketTags.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Raksha.Bcpg
{
    /// <summary>
    ///     Basic PGP packet tag types.
    /// </summary>
    public enum PacketTag
    {
        /// <summary>
        ///     Reserved - a packet tag must not have this value.
        /// </summary>
        Reserved = 0,

        /// <summary>
        ///     Public-Key Encrypted Session Key Packet.
        /// </summary>
        PublicKeyEncryptedSession = 1,

        /// <summary>
        ///     Signature Packet.
        /// </summary>
        Signature = 2,

        /// <summary>
        ///     Symmetric-Key Encrypted Session Key Packet.
        /// </summary>
        SymmetricKeyEncryptedSessionKey = 3,

        /// <summary>
        ///     One-Pass Signature Packet.
        /// </summary>
        OnePassSignature = 4,

        /// <summary>
        ///     Secret Key Packet.
        /// </summary>
        SecretKey = 5,

        /// <summary>
        ///     Public Key Packet.
        /// </summary>
        PublicKey = 6,

        /// <summary>
        ///     Secret Subkey Packet.
        /// </summary>
        SecretSubkey = 7,

        /// <summary>
        ///     Compressed Data Packet.
        /// </summary>
        CompressedData = 8,

        /// <summary>
        ///     Symmetrically Encrypted Data Packet.
        /// </summary>
        SymmetricKeyEncrypted = 9,

        /// <summary>
        ///     Marker Packet.
        /// </summary>
        Marker = 10,

        /// <summary>
        ///     Literal Data Packet.
        /// </summary>
        LiteralData = 11,

        /// <summary>
        ///     Trust Packet.
        /// </summary>
        Trust = 12,

        /// <summary>
        ///     User ID Packet.
        /// </summary>
        UserId = 13,

        /// <summary>
        ///     Public Subkey Packet.
        /// </summary>
        PublicSubkey = 14,

        /// <summary>
        ///     User attribute.
        /// </summary>
        UserAttribute = 17,

        /// <summary>
        ///     Symmetric encrypted, integrity protected.
        /// </summary>
        SymmetricEncryptedIntegrityProtected = 18,

        /// <summary>
        ///     Modification detection code.
        /// </summary>
        ModificationDetectionCode = 19,

        /// <summary>
        ///     Private or Experimental Value.
        /// </summary>
        Experimental1 = 60,

        /// <summary>
        ///     Private or Experimental Value.
        /// </summary>
        Experimental2 = 61,

        /// <summary>
        ///     Private or Experimental Value.
        /// </summary>
        Experimental3 = 62,

        /// <summary>
        ///     Private or Experimental Value.
        /// </summary>
        Experimental4 = 63
    }
}
