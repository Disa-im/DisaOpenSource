// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArmoredOutputStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using System.IO;
using Raksha.Utilities;
using Raksha.Utilities.IO;

namespace Raksha.Bcpg
{
    /// <summary>
    ///     Basic output stream.
    /// </summary>
    public class ArmoredOutputStream : BaseOutputStream
    {
        private const string HeaderStart = "-----BEGIN PGP ";
        private const string HeaderTail = "-----";
        private const string FooterStart = "-----END PGP ";
        private const string FooterTail = "-----";

        private static readonly byte[] EncodingTable =
        {
            (byte) 'A', (byte) 'B', (byte) 'C', (byte) 'D', (byte) 'E', (byte) 'F', (byte) 'G', (byte) 'H', (byte) 'I',
            (byte) 'J', (byte) 'K', (byte) 'L', (byte) 'M', (byte) 'N', (byte) 'O', (byte) 'P', (byte) 'Q', (byte) 'R', (byte) 'S', (byte) 'T', (byte) 'U', (byte) 'V',
            (byte) 'W', (byte) 'X', (byte) 'Y', (byte) 'Z', (byte) 'a', (byte) 'b', (byte) 'c', (byte) 'd', (byte) 'e', (byte) 'f', (byte) 'g', (byte) 'h', (byte) 'i',
            (byte) 'j', (byte) 'k', (byte) 'l', (byte) 'm', (byte) 'n', (byte) 'o', (byte) 'p', (byte) 'q', (byte) 'r', (byte) 's', (byte) 't', (byte) 'u', (byte) 'v',
            (byte) 'w', (byte) 'x', (byte) 'y', (byte) 'z', (byte) '0', (byte) '1', (byte) '2', (byte) '3', (byte) '4', (byte) '5', (byte) '6', (byte) '7', (byte) '8',
            (byte) '9', (byte) '+', (byte) '/'
        };

        /**
         * encode the input data producing a base 64 encoded byte array.
         */

        private static readonly string NewLine = Platform.NewLine;

        private static readonly string Version = "BCPG C# v" + Platform.ThisAssembly.GetName().Version;
        private readonly int[] _buf = new int[3];
        private readonly Crc24 _crc = new Crc24();

        private readonly IDictionary _headers;
        private readonly Stream _outStream;
        private int _bufPtr;
        private int _chunkCount;
        private bool _clearText;
        private int _lastb;
        private bool _newLine;
        private bool _start = true;
        private string _type;

        public ArmoredOutputStream(Stream outStream)
        {
            _outStream = outStream;
            _headers = Platform.CreateHashtable();
            _headers["Version"] = Version;
        }

        public ArmoredOutputStream(Stream outStream, IDictionary headers)
        {
            _outStream = outStream;
            _headers = Platform.CreateHashtable(headers);
            _headers["Version"] = Version;
        }

        private static void Encode(Stream outStream, int[] data, int len)
        {
            Debug.Assert(len > 0);
            Debug.Assert(len < 4);

            var bs = new byte[4];
            int d1 = data[0];
            bs[0] = EncodingTable[(d1 >> 2) & 0x3f];

            switch (len)
            {
                case 1:
                {
                    bs[1] = EncodingTable[(d1 << 4) & 0x3f];
                    bs[2] = (byte) '=';
                    bs[3] = (byte) '=';
                    break;
                }
                case 2:
                {
                    int d2 = data[1];
                    bs[1] = EncodingTable[((d1 << 4) | (d2 >> 4)) & 0x3f];
                    bs[2] = EncodingTable[(d2 << 2) & 0x3f];
                    bs[3] = (byte) '=';
                    break;
                }
                case 3:
                {
                    int d2 = data[1];
                    int d3 = data[2];
                    bs[1] = EncodingTable[((d1 << 4) | (d2 >> 4)) & 0x3f];
                    bs[2] = EncodingTable[((d2 << 2) | (d3 >> 6)) & 0x3f];
                    bs[3] = EncodingTable[d3 & 0x3f];
                    break;
                }
            }

            outStream.Write(bs, 0, bs.Length);
        }

        /**
         * Set an additional header entry.
         *
         * @param name the name of the header entry.
         * @param v the value of the header entry.
         */

        public void SetHeader(string name, string v)
        {
            _headers[name] = v;
        }

        /**
         * Reset the headers to only contain a Version string.
         */

        public void ResetHeaders()
        {
            _headers.Clear();
            _headers["Version"] = Version;
        }

        /**
         * Start a clear text signed message.
         * @param hashAlgorithm
         */

        public void BeginClearText(HashAlgorithmTag hashAlgorithm)
        {
            string hash;

            switch (hashAlgorithm)
            {
                case HashAlgorithmTag.Sha1:
                    hash = "SHA1";
                    break;
                case HashAlgorithmTag.Sha256:
                    hash = "SHA256";
                    break;
                case HashAlgorithmTag.Sha384:
                    hash = "SHA384";
                    break;
                case HashAlgorithmTag.Sha512:
                    hash = "SHA512";
                    break;
                case HashAlgorithmTag.MD2:
                    hash = "MD2";
                    break;
                case HashAlgorithmTag.MD5:
                    hash = "MD5";
                    break;
                case HashAlgorithmTag.RipeMD160:
                    hash = "RIPEMD160";
                    break;
                default:
                    throw new IOException("unknown hash algorithm tag in beginClearText: " + hashAlgorithm);
            }

            DoWrite("-----BEGIN PGP SIGNED MESSAGE-----" + NewLine);
            DoWrite("Hash: " + hash + NewLine + NewLine);

            _clearText = true;
            _newLine = true;
            _lastb = 0;
        }

        public void EndClearText()
        {
            _clearText = false;
        }

        public override void WriteByte(byte b)
        {
            if (_clearText)
            {
                _outStream.WriteByte(b);

                if (_newLine)
                {
                    if (!(b == '\n' && _lastb == '\r'))
                    {
                        _newLine = false;
                    }
                    if (b == '-')
                    {
                        _outStream.WriteByte((byte) ' ');
                        _outStream.WriteByte((byte) '-'); // dash escape
                    }
                }
                if (b == '\r' || (b == '\n' && _lastb != '\r'))
                {
                    _newLine = true;
                }
                _lastb = b;
                return;
            }

            if (_start)
            {
                bool newPacket = (b & 0x40) != 0;

                int tag;
                if (newPacket)
                {
                    tag = b & 0x3f;
                }
                else
                {
                    tag = (b & 0x3f) >> 2;
                }

                switch ((PacketTag) tag)
                {
                    case PacketTag.PublicKey:
                        _type = "PUBLIC KEY BLOCK";
                        break;
                    case PacketTag.SecretKey:
                        _type = "PRIVATE KEY BLOCK";
                        break;
                    case PacketTag.Signature:
                        _type = "SIGNATURE";
                        break;
                    default:
                        _type = "MESSAGE";
                        break;
                }

                DoWrite(HeaderStart + _type + HeaderTail + NewLine);
                WriteHeaderEntry("Version", (string) _headers["Version"]);

                foreach (DictionaryEntry de in _headers)
                {
                    var k = (string) de.Key;
                    if (k != "Version")
                    {
                        var v = (string) de.Value;
                        WriteHeaderEntry(k, v);
                    }
                }

                DoWrite(NewLine);

                _start = false;
            }

            if (_bufPtr == 3)
            {
                Encode(_outStream, _buf, _bufPtr);
                _bufPtr = 0;
                if ((++_chunkCount & 0xf) == 0)
                {
                    DoWrite(NewLine);
                }
            }

            _crc.Update(b);
            _buf[_bufPtr++] = b & 0xff;
        }

        /**
         * <b>Note</b>: close does nor close the underlying stream. So it is possible to write
         * multiple objects using armoring to a single stream.
         */

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing || _type == null)
                {
                    return;
                }

                if (_bufPtr > 0)
                {
                    Encode(_outStream, _buf, _bufPtr);
                }

                DoWrite(NewLine + '=');

                int crcV = _crc.Value;

                _buf[0] = ((crcV >> 16) & 0xff);
                _buf[1] = ((crcV >> 8) & 0xff);
                _buf[2] = (crcV & 0xff);

                Encode(_outStream, _buf, 3);

                DoWrite(NewLine);
                DoWrite(FooterStart);
                DoWrite(_type);
                DoWrite(FooterTail);
                DoWrite(NewLine);

                _outStream.Flush();

                _type = null;
                _start = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void WriteHeaderEntry(string name, string v)
        {
            DoWrite(name + ": " + v + NewLine);
        }

        private void DoWrite(string s)
        {
            byte[] bs = Strings.ToAsciiByteArray(s);
            _outStream.Write(bs, 0, bs.Length);
        }
    }
}
