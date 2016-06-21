// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BcpgInputStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Raksha.Utilities.IO;

namespace Raksha.Bcpg
{
    /// <remarks>Reader for PGP objects.</remarks>
    public class BcpgInputStream : BaseInputStream
    {
        private readonly Stream _in;
        private bool _next;
        private int _nextB;

        private BcpgInputStream(Stream inputStream)
        {
            _in = inputStream;
        }

        internal static BcpgInputStream Wrap(Stream inStr)
        {
            if (inStr is BcpgInputStream)
            {
                return (BcpgInputStream) inStr;
            }

            return new BcpgInputStream(inStr);
        }

        public override int ReadByte()
        {
            if (_next)
            {
                _next = false;
                return _nextB;
            }

            return _in.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Strangely, when count == 0, we should still attempt to read a byte
//			if (count == 0)
//				return 0;

            if (!_next)
            {
                return _in.Read(buffer, offset, count);
            }

            // We have next byte waiting, so return it

            if (_nextB < 0)
            {
                return 0; // EndOfStream
            }

            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            buffer[offset] = (byte) _nextB;
            _next = false;

            return 1;
        }

        public byte[] ReadAll()
        {
            return Streams.ReadAll(this);
        }

        public void ReadFully(byte[] buffer, int off, int len)
        {
            if (Streams.ReadFully(this, buffer, off, len) < len)
            {
                throw new EndOfStreamException();
            }
        }

        public void ReadFully(byte[] buffer)
        {
            ReadFully(buffer, 0, buffer.Length);
        }

        /// <summary>Returns the next packet tag in the stream.</summary>
        public PacketTag NextPacketTag()
        {
            if (!_next)
            {
                try
                {
                    _nextB = _in.ReadByte();
                }
                catch (EndOfStreamException)
                {
                    _nextB = -1;
                }

                _next = true;
            }

            if (_nextB >= 0)
            {
                if ((_nextB & 0x40) != 0) // new
                {
                    return (PacketTag) (_nextB & 0x3f);
                }
                return (PacketTag) ((_nextB & 0x3f) >> 2);
            }

            return (PacketTag) _nextB;
        }

        public Packet ReadPacket()
        {
            int hdr = ReadByte();

            if (hdr < 0)
            {
                return null;
            }

            if ((hdr & 0x80) == 0)
            {
                throw new IOException("invalid header encountered");
            }

            bool newPacket = (hdr & 0x40) != 0;
            PacketTag tag;
            int bodyLen = 0;
            bool partial = false;

            if (newPacket)
            {
                tag = (PacketTag) (hdr & 0x3f);

                int l = ReadByte();

                if (l < 192)
                {
                    bodyLen = l;
                }
                else if (l <= 223)
                {
                    int b = _in.ReadByte();
                    bodyLen = ((l - 192) << 8) + (b) + 192;
                }
                else if (l == 255)
                {
                    bodyLen = (_in.ReadByte() << 24) | (_in.ReadByte() << 16) | (_in.ReadByte() << 8) | _in.ReadByte();
                }
                else
                {
                    partial = true;
                    bodyLen = 1 << (l & 0x1f);
                }
            }
            else
            {
                int lengthType = hdr & 0x3;

                tag = (PacketTag) ((hdr & 0x3f) >> 2);

                switch (lengthType)
                {
                    case 0:
                        bodyLen = ReadByte();
                        break;
                    case 1:
                        bodyLen = (ReadByte() << 8) | ReadByte();
                        break;
                    case 2:
                        bodyLen = (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte();
                        break;
                    case 3:
                        partial = true;
                        break;
                    default:
                        throw new IOException("unknown length type encountered");
                }
            }

            BcpgInputStream objStream;
            if (bodyLen == 0 && partial)
            {
                objStream = this;
            }
            else
            {
                var pis = new PartialInputStream(this, partial, bodyLen);
                objStream = new BcpgInputStream(pis);
            }

            switch (tag)
            {
                case PacketTag.Reserved:
                    return new InputStreamPacket(objStream);
                case PacketTag.PublicKeyEncryptedSession:
                    return new PublicKeyEncSessionPacket(objStream);
                case PacketTag.Signature:
                    return new SignaturePacket(objStream);
                case PacketTag.SymmetricKeyEncryptedSessionKey:
                    return new SymmetricKeyEncSessionPacket(objStream);
                case PacketTag.OnePassSignature:
                    return new OnePassSignaturePacket(objStream);
                case PacketTag.SecretKey:
                    return new SecretKeyPacket(objStream);
                case PacketTag.PublicKey:
                    return new PublicKeyPacket(objStream);
                case PacketTag.SecretSubkey:
                    return new SecretSubkeyPacket(objStream);
                case PacketTag.CompressedData:
                    return new CompressedDataPacket(objStream);
                case PacketTag.SymmetricKeyEncrypted:
                    return new SymmetricEncDataPacket(objStream);
                case PacketTag.Marker:
                    return new MarkerPacket(objStream);
                case PacketTag.LiteralData:
                    return new LiteralDataPacket(objStream);
                case PacketTag.Trust:
                    return new TrustPacket(objStream);
                case PacketTag.UserId:
                    return new UserIdPacket(objStream);
                case PacketTag.UserAttribute:
                    return new UserAttributePacket(objStream);
                case PacketTag.PublicSubkey:
                    return new PublicSubkeyPacket(objStream);
                case PacketTag.SymmetricEncryptedIntegrityProtected:
                    return new SymmetricEncIntegrityPacket(objStream);
                case PacketTag.ModificationDetectionCode:
                    return new ModDetectionCodePacket(objStream);
                case PacketTag.Experimental1:
                case PacketTag.Experimental2:
                case PacketTag.Experimental3:
                case PacketTag.Experimental4:
                    return new ExperimentalPacket(tag, objStream);
                default:
                    throw new IOException("unknown packet type encountered: " + tag);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing)
                {
                    return;
                }
                _in.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        ///     A stream that overlays our input stream, allowing the user to only read a segment of it.
        ///     NB: dataLength will be negative if the segment length is in the upper range above 2**31.
        /// </summary>
        private class PartialInputStream : BaseInputStream
        {
            private readonly BcpgInputStream _in;
            private int _dataLength;
            private bool _partial;

            internal PartialInputStream(BcpgInputStream bcpgIn, bool partial, int dataLength)
            {
                _in = bcpgIn;
                _partial = partial;
                _dataLength = dataLength;
            }

            public override int ReadByte()
            {
                do
                {
                    if (_dataLength != 0)
                    {
                        int ch = _in.ReadByte();
                        if (ch < 0)
                        {
                            throw new EndOfStreamException("Premature end of stream in PartialInputStream");
                        }
                        _dataLength--;
                        return ch;
                    }
                } while (_partial && ReadPartialDataLength() >= 0);

                return -1;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                do
                {
                    if (_dataLength != 0)
                    {
                        int readLen = (_dataLength > count || _dataLength < 0) ? count : _dataLength;
                        int len = _in.Read(buffer, offset, readLen);
                        if (len < 1)
                        {
                            throw new EndOfStreamException("Premature end of stream in PartialInputStream");
                        }
                        _dataLength -= len;
                        return len;
                    }
                } while (_partial && ReadPartialDataLength() >= 0);

                return 0;
            }

            private int ReadPartialDataLength()
            {
                int l = _in.ReadByte();

                if (l < 0)
                {
                    return -1;
                }

                _partial = false;

                if (l < 192)
                {
                    _dataLength = l;
                }
                else if (l <= 223)
                {
                    _dataLength = ((l - 192) << 8) + (_in.ReadByte()) + 192;
                }
                else if (l == 255)
                {
                    _dataLength = (_in.ReadByte() << 24) | (_in.ReadByte() << 16) | (_in.ReadByte() << 8) | _in.ReadByte();
                }
                else
                {
                    _partial = true;
                    _dataLength = 1 << (l & 0x1f);
                }

                return 0;
            }
        }
    }
}
