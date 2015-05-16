// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WrappedGeneratorStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using Raksha.Asn1.Utilities;

namespace Raksha.Bcpg.OpenPgp
{
    public class WrappedGeneratorStream : FilterStream
    {
        private readonly IStreamGenerator _gen;

        public WrappedGeneratorStream(IStreamGenerator gen, Stream str) : base(str)
        {
            _gen = gen;
        }

        protected override void Dispose(bool disposing)
        {
            _gen.Close();
        }
    }
}
