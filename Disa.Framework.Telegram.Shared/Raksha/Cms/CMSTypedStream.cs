// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CMSTypedStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Raksha.Asn1.Pkcs;
using Raksha.Asn1.Utilities;
using Raksha.Utilities.IO;

namespace Raksha.Cms
{
    public class CmsTypedStream : IDisposable
    {
        private const int BufferSize = 32*1024;

        private readonly Stream _in;
        private readonly string _oid;

        public CmsTypedStream(Stream inStream) : this(PkcsObjectIdentifiers.Data.Id, inStream, BufferSize)
        {
        }

        public CmsTypedStream(string oid, Stream inStream) : this(oid, inStream, BufferSize)
        {
        }

        public CmsTypedStream(string oid, Stream inStream, int bufSize)
        {
            _oid = oid;
            _in = new FullReaderStream(inStream);
        }

        public string ContentType
        {
            get { return _oid; }
        }

        public Stream ContentStream
        {
            get { return _in; }
        }

        public void Drain()
        {
            Streams.Drain(_in);
            _in.Dispose();
        }

        private class FullReaderStream : FilterStream
        {
            internal FullReaderStream(Stream input) : base(input)
            {
            }

            public override int Read(byte[] buf, int off, int len)
            {
                return Streams.ReadFully(base.Stream, buf, off, len);
            }
        }

        public void Dispose()
        {
            Drain();
        }
    }
}
