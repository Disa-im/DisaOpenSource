// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DigestStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Raksha.Crypto.IO
{
    public class DigestStream : Stream
    {
        protected readonly IDigest InDigest;
        protected readonly IDigest OutDigest;
        protected readonly Stream Stream;

        public DigestStream(Stream stream, IDigest readDigest, IDigest writeDigest)
        {
            Stream = stream;
            InDigest = readDigest;
            OutDigest = writeDigest;
        }

        public override bool CanRead
        {
            get { return Stream.CanRead; }
        }

        public override bool CanWrite
        {
            get { return Stream.CanWrite; }
        }

        public override bool CanSeek
        {
            get { return Stream.CanSeek; }
        }

        public override long Length
        {
            get { return Stream.Length; }
        }

        public override long Position
        {
            get { return Stream.Position; }
            set { Stream.Position = value; }
        }

        public virtual IDigest ReadDigest()
        {
            return InDigest;
        }

        public virtual IDigest WriteDigest()
        {
            return OutDigest;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = Stream.Read(buffer, offset, count);
            if (InDigest != null)
            {
                if (n > 0)
                {
                    InDigest.BlockUpdate(buffer, offset, n);
                }
            }
            return n;
        }

        public override int ReadByte()
        {
            int b = Stream.ReadByte();
            if (InDigest != null)
            {
                if (b >= 0)
                {
                    InDigest.Update((byte) b);
                }
            }
            return b;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (OutDigest != null)
            {
                if (count > 0)
                {
                    OutDigest.BlockUpdate(buffer, offset, count);
                }
            }
            Stream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte b)
        {
            if (OutDigest != null)
            {
                OutDigest.Update(b);
            }
            Stream.WriteByte(b);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing)
                {
                    return;
                }

                Stream.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            Stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Stream.Seek(offset, origin);
        }

        public override void SetLength(long length)
        {
            Stream.SetLength(length);
        }
    }
}
