// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PgpCompressedDataGenerator.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Raksha.Apache.Bzip2;
using Raksha.Utilities.Zlib;

namespace Raksha.Bcpg.OpenPgp
{
    /// <remarks>Class for producing compressed data packets.</remarks>
    public class PgpCompressedDataGenerator : IStreamGenerator
    {
        private readonly CompressionAlgorithmTag _algorithm;
        private readonly int _compression;

        private Stream _dOut;
        private BcpgOutputStream _pkOut;

        public PgpCompressedDataGenerator(CompressionAlgorithmTag algorithm) : this(algorithm, JZlib.Z_DEFAULT_COMPRESSION)
        {
        }

        public PgpCompressedDataGenerator(CompressionAlgorithmTag algorithm, int compression)
        {
            switch (algorithm)
            {
                case CompressionAlgorithmTag.Uncompressed:
                case CompressionAlgorithmTag.Zip:
                case CompressionAlgorithmTag.ZLib:
                case CompressionAlgorithmTag.BZip2:
                    break;
                default:
                    throw new ArgumentException("unknown compression algorithm", "algorithm");
            }

            if (compression != JZlib.Z_DEFAULT_COMPRESSION)
            {
                if ((compression < JZlib.Z_NO_COMPRESSION) || (compression > JZlib.Z_BEST_COMPRESSION))
                {
                    throw new ArgumentException("unknown compression level: " + compression);
                }
            }

            _algorithm = algorithm;
            _compression = compression;
        }

        /// <summary>Close the compressed object.</summary>
        /// summary>
        public void Close()
        {
            if (_dOut != null)
            {
                if (_dOut != _pkOut)
                {
                    _dOut.Flush();
                    _dOut.Dispose();
                }

                _dOut = null;

                _pkOut.Finish();
                _pkOut.Flush();
                _pkOut = null;
            }
        }

        /// <summary>
        ///     <para>
        ///         Return an output stream which will save the data being written to
        ///         the compressed object.
        ///     </para>
        ///     <para>
        ///         The stream created can be closed off by either calling Close()
        ///         on the stream or Close() on the generator. Closing the returned
        ///         stream does not close off the Stream parameter <c>outStr</c>.
        ///     </para>
        /// </summary>
        /// <param name="outStr">Stream to be used for output.</param>
        /// <returns>A Stream for output of the compressed data.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="IOException"></exception>
        public Stream Open(Stream outStr)
        {
            if (_dOut != null)
            {
                throw new InvalidOperationException("generator already in open state");
            }
            if (outStr == null)
            {
                throw new ArgumentNullException("outStr");
            }

            _pkOut = new BcpgOutputStream(outStr, PacketTag.CompressedData);

            DoOpen();

            return new WrappedGeneratorStream(this, _dOut);
        }

        /// <summary>
        ///     <p>
        ///         Return an output stream which will compress the data as it is written to it.
        ///         The stream will be written out in chunks according to the size of the passed in buffer.
        ///     </p>
        ///     <p>
        ///         The stream created can be closed off by either calling Close()
        ///         on the stream or Close() on the generator. Closing the returned
        ///         stream does not close off the Stream parameter <c>outStr</c>.
        ///     </p>
        ///     <p>
        ///         <b>Note</b>: if the buffer is not a power of 2 in length only the largest power of 2
        ///         bytes worth of the buffer will be used.
        ///     </p>
        ///     <p>
        ///         <b>Note</b>: using this may break compatibility with RFC 1991 compliant tools.
        ///         Only recent OpenPGP implementations are capable of accepting these streams.
        ///     </p>
        /// </summary>
        /// <param name="outStr">Stream to be used for output.</param>
        /// <param name="buffer">The buffer to use.</param>
        /// <returns>A Stream for output of the compressed data.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="PgpException"></exception>
        public Stream Open(Stream outStr, byte[] buffer)
        {
            if (_dOut != null)
            {
                throw new InvalidOperationException("generator already in open state");
            }
            if (outStr == null)
            {
                throw new ArgumentNullException("outStr");
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            _pkOut = new BcpgOutputStream(outStr, PacketTag.CompressedData, buffer);

            DoOpen();

            return new WrappedGeneratorStream(this, _dOut);
        }

        private void DoOpen()
        {
            _pkOut.WriteByte((byte) _algorithm);

            switch (_algorithm)
            {
                case CompressionAlgorithmTag.Uncompressed:
                    _dOut = _pkOut;
                    break;
                case CompressionAlgorithmTag.Zip:
                    _dOut = new SafeZOutputStream(_pkOut, _compression, true);
                    break;
                case CompressionAlgorithmTag.ZLib:
                    _dOut = new SafeZOutputStream(_pkOut, _compression, false);
                    break;
                case CompressionAlgorithmTag.BZip2:
                    _dOut = new SafeCBZip2OutputStream(_pkOut);
                    break;
                default:
                    // Constructor should guard against this possibility
                    throw new InvalidOperationException();
            }
        }

        private class SafeCBZip2OutputStream : CBZip2OutputStream
        {
            public SafeCBZip2OutputStream(Stream output) : base(output)
            {
            }

            protected override void Dispose(bool disposing)
            {
                Finish();
            }
        }

        private class SafeZOutputStream : ZOutputStream
        {
            public SafeZOutputStream(Stream output, int level, bool nowrap) : base(output, level, nowrap)
            {
            }

            protected override void Dispose(bool disposing)
            {
                Finish();
                End();
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
