// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TlsCompression.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Raksha.Crypto.Tls
{
    public interface ITlsCompression
    {
        Stream Compress(Stream output);

        Stream Decompress(Stream output);
    }
}
