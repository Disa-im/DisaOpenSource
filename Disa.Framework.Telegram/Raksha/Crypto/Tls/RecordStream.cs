// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RecordStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Raksha.Crypto.Tls
{
    /// <summary>
    ///     An implementation of the TLS 1.0 record layer.
    /// </summary>
    internal class RecordStream
    {
        private readonly MemoryStream _buffer = new MemoryStream();
        private readonly TlsProtocolHandler _handler;
        private readonly CombinedHash _hash;
        private readonly Stream _inStr;
        private readonly Stream _outStr;
        private TlsCipher _readCipher;
        private ITlsCompression _readCompression;
        private TlsCipher _writeCipher;
        private ITlsCompression _writeCompression;

        internal RecordStream(TlsProtocolHandler handler, Stream inStr, Stream outStr)
        {
            _handler = handler;
            _inStr = inStr;
            _outStr = outStr;
            _hash = new CombinedHash();
            _readCompression = new TlsNullCompression();
            _writeCompression = _readCompression;
            _readCipher = new TlsNullCipher();
            _writeCipher = _readCipher;
        }

        internal void ClientCipherSpecDecided(ITlsCompression tlsCompression, TlsCipher tlsCipher)
        {
            _writeCompression = tlsCompression;
            _writeCipher = tlsCipher;
        }

        internal void ServerClientSpecReceived()
        {
            _readCompression = _writeCompression;
            _readCipher = _writeCipher;
        }

        public void ReadData()
        {
            var type = (ContentType) TlsUtilities.ReadUint8(_inStr);
            TlsUtilities.CheckVersion(_inStr, _handler);
            int size = TlsUtilities.ReadUint16(_inStr);
            byte[] buf = DecodeAndVerify(type, _inStr, size);
            _handler.ProcessData(type, buf, 0, buf.Length);
        }

        internal byte[] DecodeAndVerify(ContentType type, Stream inStr, int len)
        {
            var buf = new byte[len];
            TlsUtilities.ReadFully(buf, inStr);
            byte[] decoded = _readCipher.DecodeCiphertext(type, buf, 0, buf.Length);

            Stream cOut = _readCompression.Decompress(_buffer);

            if (cOut == _buffer)
            {
                return decoded;
            }

            cOut.Write(decoded, 0, decoded.Length);
            cOut.Flush();
            byte[] contents = _buffer.ToArray();
            _buffer.SetLength(0);
            return contents;
        }

        internal void WriteMessage(ContentType type, byte[] message, int offset, int len)
        {
            if (type == ContentType.handshake)
            {
                UpdateHandshakeData(message, offset, len);
            }

            Stream cOut = _writeCompression.Compress(_buffer);

            byte[] ciphertext;
            if (cOut == _buffer)
            {
                ciphertext = _writeCipher.EncodePlaintext(type, message, offset, len);
            }
            else
            {
                cOut.Write(message, offset, len);
                cOut.Flush();
                ciphertext = _writeCipher.EncodePlaintext(type, _buffer.ToArray(), 0, (int) _buffer.Position);
                _buffer.SetLength(0);
            }

            var writeMessage = new byte[ciphertext.Length + 5];
            TlsUtilities.WriteUint8((byte) type, writeMessage, 0);
            TlsUtilities.WriteVersion(writeMessage, 1);
            TlsUtilities.WriteUint16(ciphertext.Length, writeMessage, 3);
            Array.Copy(ciphertext, 0, writeMessage, 5, ciphertext.Length);
            _outStr.Write(writeMessage, 0, writeMessage.Length);
            _outStr.Flush();
        }

        internal void UpdateHandshakeData(byte[] message, int offset, int len)
        {
            _hash.BlockUpdate(message, offset, len);
        }

        internal byte[] GetCurrentHash()
        {
            return DoFinal(new CombinedHash(_hash));
        }

        internal void Close()
        {
            IOException e = null;
            try
            {
                _inStr.Dispose();
            }
            catch (IOException ex)
            {
                e = ex;
            }

            try
            {
                // NB: This is harmless if outStr == inStr
                _outStr.Dispose();
            }
            catch (IOException ex)
            {
                e = ex;
            }

            if (e != null)
            {
                throw e;
            }
        }

        internal void Flush()
        {
            _outStr.Flush();
        }

        private static byte[] DoFinal(CombinedHash ch)
        {
            var bs = new byte[ch.GetDigestSize()];
            ch.DoFinal(bs, 0);
            return bs;
        }
    }
}
