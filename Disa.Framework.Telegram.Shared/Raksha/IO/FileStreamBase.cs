// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Raksha.IO
{
    public abstract class FileStreamBase : Stream
    {
        protected abstract void Initialize(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize);

        public static FileStreamBase Create(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
        {
            return new Net45FileStream(new FileStream(path, mode, access, share, bufferSize));
        }
    }
}
