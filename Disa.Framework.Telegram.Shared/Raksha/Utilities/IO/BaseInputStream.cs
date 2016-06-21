// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseInputStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Raksha.Utilities.IO
{
    public abstract class BaseInputStream : Stream
    {
        private bool _closed;

        public override sealed bool CanRead
        {
            get { return !_closed; }
        }

        public override sealed bool CanSeek
        {
            get { return false; }
        }

        public override sealed bool CanWrite
        {
            get { return false; }
        }

        public override sealed long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override sealed long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        protected override void Dispose(bool disposing)
        {
            _closed = true;
            base.Dispose(disposing);
        }

        public override sealed void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int pos = offset;
            try
            {
                int end = offset + count;
                while (pos < end)
                {
                    int b = ReadByte();
                    if (b == -1)
                    {
                        break;
                    }
                    buffer[pos++] = (byte) b;
                }
            }
            catch (IOException)
            {
                if (pos == offset)
                {
                    throw;
                }
            }
            return pos - offset;
        }

        public override sealed long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override sealed void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override sealed void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
