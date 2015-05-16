// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CMSProcessableFile.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using Raksha.IO;
using Raksha.Utilities.IO;

namespace Raksha.Cms
{
    /**
    * a holding class for a file of data to be processed.
    */

    public class CmsProcessableFile : CmsProcessable, ICmsReadable
    {
        private const int DefaultBufSize = 32*1024;

        private readonly int _bufSize;
        private readonly IFileInfo _file;

        public CmsProcessableFile(IFileInfo file) : this(file, DefaultBufSize)
        {
        }

        public CmsProcessableFile(IFileInfo file, int bufSize)
        {
            _file = file;
            _bufSize = bufSize;
        }

        public virtual void Write(Stream zOut)
        {
            using (Stream inStr = GetInputStream())
            {
                Streams.PipeAll(inStr, zOut);
            }
        }

        /// <returns>The file handle</returns>
        public virtual object GetContent()
        {
            return _file;
        }

        public virtual Stream GetInputStream()
        {
            return FileStreamBase.Create(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, _bufSize);
        }
    }
}
