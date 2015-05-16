// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CMSCompressedDataStreamGenerator.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using Raksha.Asn1;
using Raksha.Asn1.Cms;
using Raksha.Asn1.X509;
using Raksha.Utilities.IO;
using Raksha.Utilities.Zlib;

namespace Raksha.Cms
{
    /**
    * General class for generating a compressed CMS message stream.
    * <p>
    * A simple example of usage.
    * </p>
    * <pre>
    *      CMSCompressedDataStreamGenerator gen = new CMSCompressedDataStreamGenerator();
    *
    *      Stream cOut = gen.Open(outputStream, CMSCompressedDataStreamGenerator.ZLIB);
    *
    *      cOut.Write(data);
    *
    *      cOut.Close();
    * </pre>
    */

    public class CmsCompressedDataStreamGenerator
    {
        public const string ZLib = "1.2.840.113549.1.9.16.3.8";

        private int _bufferSize;

        /**
        * Set the underlying string size for encapsulated data
        *
        * @param bufferSize length of octet strings to buffer the data.
        */

        public void SetBufferSize(int bufferSize)
        {
            _bufferSize = bufferSize;
        }

        public Stream Open(Stream outStream, string compressionOid)
        {
            return Open(outStream, CmsObjectIdentifiers.Data.Id, compressionOid);
        }

        public Stream Open(Stream outStream, string contentOid, string compressionOid)
        {
            var sGen = new BerSequenceGenerator(outStream);

            sGen.AddObject(CmsObjectIdentifiers.CompressedData);

            //
            // Compressed Data
            //
            var cGen = new BerSequenceGenerator(sGen.GetRawOutputStream(), 0, true);

            // CMSVersion
            cGen.AddObject(new DerInteger(0));

            // CompressionAlgorithmIdentifier
            cGen.AddObject(new AlgorithmIdentifier(new DerObjectIdentifier(ZLib)));

            //
            // Encapsulated ContentInfo
            //
            var eiGen = new BerSequenceGenerator(cGen.GetRawOutputStream());

            eiGen.AddObject(new DerObjectIdentifier(contentOid));

            Stream octetStream = CmsUtilities.CreateBerOctetOutputStream(eiGen.GetRawOutputStream(), 0, true, _bufferSize);

            return new CmsCompressedOutputStream(new ZOutputStream(octetStream, JZlib.Z_DEFAULT_COMPRESSION), sGen, cGen, eiGen);
        }

        private class CmsCompressedOutputStream : BaseOutputStream
        {
            private readonly BerSequenceGenerator _cGen;
            private readonly BerSequenceGenerator _eiGen;
            private readonly ZOutputStream _out;
            private readonly BerSequenceGenerator _sGen;

            internal CmsCompressedOutputStream(ZOutputStream outStream, BerSequenceGenerator sGen, BerSequenceGenerator cGen, BerSequenceGenerator eiGen)
            {
                _out = outStream;
                _sGen = sGen;
                _cGen = cGen;
                _eiGen = eiGen;
            }

            public override void WriteByte(byte b)
            {
                _out.WriteByte(b);
            }

            public override void Write(byte[] bytes, int off, int len)
            {
                _out.Write(bytes, off, len);
            }

            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (!disposing)
                    {
                        return;
                    }
                    _out.Dispose();

                    // TODO Parent context(s) should really be be closed explicitly

                    _eiGen.Close();
                    _cGen.Close();
                    _sGen.Close();
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
    }
}
