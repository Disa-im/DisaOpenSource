/*
Copyright (c) 2001 Lapo Luchini.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

  1. Redistributions of source code must retain the above copyright notice,
     this list of conditions and the following disclaimer.

  2. Redistributions in binary form must reproduce the above copyright 
     notice, this list of conditions and the following disclaimer in 
     the documentation and/or other materials provided with the distribution.

  3. The names of the authors may not be used to endorse or promote products
     derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS
OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
/*
 * This program is based on zlib-1.1.3, so all credit should go authors
 * Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
 * and contributors of zlib.
 */
/* This file is a port of jzlib v1.0.7, com.jcraft.jzlib.ZOutputStream.java
 */

using System;
using System.Diagnostics;
using System.IO;

namespace Raksha.Utilities.Zlib
{
    public class ZOutputStream : Stream
    {
        private const int BufferSize = 512;

        // TODO Allow custom buf
        protected readonly byte[] Buf = new byte[BufferSize];
        protected readonly byte[] Buf1 = new byte[1];
        protected bool Closed;
        protected bool Compress;
        protected int FlushLevel = JZlib.Z_NO_FLUSH;

        protected Stream output;
        protected ZStream z = new ZStream();

        public ZOutputStream(Stream output)
        {
            Debug.Assert(output.CanWrite);

            this.output = output;
            z.inflateInit();
            Compress = false;
        }

        public ZOutputStream(Stream output, int level) : this(output, level, false)
        {
        }

        public ZOutputStream(Stream output, int level, bool nowrap)
        {
            Debug.Assert(output.CanWrite);

            this.output = output;
            z.deflateInit(level, nowrap);
            Compress = true;
        }

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
            get { return !Closed; }
        }

        public virtual int FlushMode
        {
            get { return FlushLevel; }
            set { FlushLevel = value; }
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

        public virtual long TotalIn
        {
            get { return z.total_in; }
        }

        public virtual long TotalOut
        {
            get { return z.total_out; }
        }
        
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing || Closed)
                {
                    return;
                }
                Closed = true;

                try
                {
                    try
                    {
                        Finish();
                    }
                    catch (IOException)
                    {
                        // Ignore
                    }
                }
                finally
                {
                    End();
                    output.Dispose();
                    output = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public virtual void End()
        {
            if (z == null)
            {
                return;
            }
            if (Compress)
            {
                z.deflateEnd();
            }
            else
            {
                z.inflateEnd();
            }
            z.free();
            z = null;
        }

        public virtual void Finish()
        {
            do
            {
                z.next_out = Buf;
                z.next_out_index = 0;
                z.avail_out = Buf.Length;

                int err = Compress ? z.deflate(JZlib.Z_FINISH) : z.inflate(JZlib.Z_FINISH);

                if (err != JZlib.Z_STREAM_END && err != JZlib.Z_OK)
                {
                    // TODO
//					throw new ZStreamException((compress?"de":"in")+"flating: "+z.msg);
                    throw new IOException((Compress ? "de" : "in") + "flating: " + z.msg);
                }

                int count = Buf.Length - z.avail_out;
                if (count > 0)
                {
                    output.Write(Buf, 0, count);
                }
            } while (z.avail_in > 0 || z.avail_out == 0);

            Flush();
        }

        public override void Flush()
        {
            output.Flush();
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

        public override void Write(byte[] b, int off, int len)
        {
            if (len == 0)
            {
                return;
            }

            z.next_in = b;
            z.next_in_index = off;
            z.avail_in = len;

            do
            {
                z.next_out = Buf;
                z.next_out_index = 0;
                z.avail_out = Buf.Length;

                int err = Compress ? z.deflate(FlushLevel) : z.inflate(FlushLevel);

                if (err != JZlib.Z_OK)
                {
                    // TODO
//					throw new ZStreamException((compress ? "de" : "in") + "flating: " + z.msg);
                    throw new IOException((Compress ? "de" : "in") + "flating: " + z.msg);
                }

                output.Write(Buf, 0, Buf.Length - z.avail_out);
            } while (z.avail_in > 0 || z.avail_out == 0);
        }

        public override void WriteByte(byte b)
        {
            Buf1[0] = b;
            Write(Buf1, 0, 1);
        }
    }
}
