using System;
using System.IO;
using Raksha.Utilities.Date;

namespace Raksha.Bcpg
{
	/// <remarks>Basic packet for a PGP public key.</remarks>
    public class PublicKeyPacket
        : ContainedPacket //, PublicKeyAlgorithmTag
    {
		private int version;
        private long time;
        private int validDays;
        private PublicKeyAlgorithmTag algorithm;
        private IBcpgKey key;

        internal PublicKeyPacket(
            BcpgInputStream bcpgIn)
        {
            version = bcpgIn.ReadByte();

            time = ((uint)bcpgIn.ReadByte() << 24) | ((uint)bcpgIn.ReadByte() << 16)
                | ((uint)bcpgIn.ReadByte() << 8) | (uint)bcpgIn.ReadByte();

            if (version <= 3)
            {
                validDays = (bcpgIn.ReadByte() << 8) | bcpgIn.ReadByte();
            }

            algorithm = (PublicKeyAlgorithmTag) bcpgIn.ReadByte();

            switch ((PublicKeyAlgorithmTag) algorithm)
            {
                case PublicKeyAlgorithmTag.RsaEncrypt:
                case PublicKeyAlgorithmTag.RsaGeneral:
                case PublicKeyAlgorithmTag.RsaSign:
                    key = new RsaPublicBcpgKey(bcpgIn);
                    break;
                case PublicKeyAlgorithmTag.Dsa:
                    key = new DsaPublicBcpgKey(bcpgIn);
                    break;
                case PublicKeyAlgorithmTag.ElGamalEncrypt:
                case PublicKeyAlgorithmTag.ElGamalGeneral:
                    key = new ElGamalPublicBcpgKey(bcpgIn);
                    break;
                default:
                    throw new IOException("unknown PGP public key algorithm encountered");
            }
        }

		/// <summary>Construct a version 4 public key packet.</summary>
        public PublicKeyPacket(
            PublicKeyAlgorithmTag	algorithm,
            DateTime				time,
            IBcpgKey				key)
        {
			this.version = 4;
            this.time = DateTimeUtilities.DateTimeToUnixMs(time) / 1000L;
            this.algorithm = algorithm;
            this.key = key;
        }

        public int Version
        {
			get { return version; }
        }

        public PublicKeyAlgorithmTag Algorithm
        {
			get { return algorithm; }
        }

        public int ValidDays
        {
			get { return validDays; }
        }

		public DateTime GetTime()
        {
            return DateTimeUtilities.UnixMsToDateTime(time * 1000L);
        }

        public IBcpgKey Key
        {
			get { return key; }
        }

        public byte[] GetEncodedContents()
        {
            MemoryStream bOut = new MemoryStream();
            BcpgOutputStream pOut = new BcpgOutputStream(bOut);

            pOut.WriteByte((byte) version);
            pOut.WriteInt((int) time);

			if (version <= 3)
            {
                pOut.WriteShort((short) validDays);
            }

			pOut.WriteByte((byte) algorithm);

			pOut.WriteObject((BcpgObject)key);

			return bOut.ToArray();
        }

		public override void Encode(
			BcpgOutputStream bcpgOut)
		{
			bcpgOut.WritePacket(PacketTag.PublicKey, GetEncodedContents(), true);
		}
	}
}
