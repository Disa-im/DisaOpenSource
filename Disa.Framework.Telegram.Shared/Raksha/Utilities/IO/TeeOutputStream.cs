// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TeeOutputStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.IO;

namespace Raksha.Utilities.IO
{
    public class TeeOutputStream : BaseOutputStream
    {
        private readonly Stream _output, _tee;

        public TeeOutputStream(Stream output, Stream tee)
        {
            Debug.Assert(output.CanWrite);
            Debug.Assert(tee.CanWrite);

            _output = output;
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

                _output.Dispose();
                _tee.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _output.Write(buffer, offset, count);
            _tee.Write(buffer, offset, count);
        }

        public override void WriteByte(byte b)
        {
            _output.WriteByte(b);
            _tee.WriteByte(b);
        }
    }
}
