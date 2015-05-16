// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseOutputStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;

namespace Raksha.Utilities.IO
{
    public abstract class BaseOutputStream : Stream
    {
        private bool _closed;

        public override sealed bool CanRead
        {
            get { return false; }
        }

        public override sealed bool CanSeek
        {
            get { return false; }
        }

        public override sealed bool CanWrite
        {
            get { return !_closed; }
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

        public override void Flush()
        {
        }

        public override sealed int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override sealed long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override sealed void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(0 <= offset && offset <= buffer.Length);
            Debug.Assert(count >= 0);

            int end = offset + count;

            Debug.Assert(0 <= end && end <= buffer.Length);

            for (int i = offset; i < end; ++i)
            {
                WriteByte(buffer[i]);
            }
        }

        public virtual void Write(params byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }
    }
}
