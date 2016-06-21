// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TeeInputStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.IO;

namespace Raksha.Utilities.IO
{
    public class TeeInputStream : BaseInputStream
    {
        private readonly Stream _input, _tee;

        public TeeInputStream(Stream input, Stream tee)
        {
            Debug.Assert(input.CanRead);
            Debug.Assert(tee.CanWrite);

            _input = input;
            _tee = tee;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing)
                {
                    return;
                }

                _input.Dispose();
                _tee.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override int Read(byte[] buf, int off, int len)
        {
            int i = _input.Read(buf, off, len);

            if (i > 0)
            {
                _tee.Write(buf, off, i);
            }

            return i;
        }

        public override int ReadByte()
        {
            int i = _input.ReadByte();

            if (i >= 0)
            {
                _tee.WriteByte((byte) i);
            }

            return i;
        }
    }
}
