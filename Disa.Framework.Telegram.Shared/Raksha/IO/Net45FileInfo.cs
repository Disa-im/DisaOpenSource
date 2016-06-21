// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Net45FileInfo.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Raksha.IO
{
    public class Net45FileInfo : IFileInfo
    {
        private readonly FileInfo _fileInfo;

        public Net45FileInfo(string fileName)
        {
            _fileInfo = new FileInfo(fileName);
        }

        protected Net45FileInfo(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }

        public string FullName
        {
            get { return _fileInfo.FullName; }
        }

        public string Name
        {
            get { return _fileInfo.Name; }
        }

        public long Length
        {
            get { return _fileInfo.Length; }
        }

        public DateTime LastWriteTime
        {
            get { return _fileInfo.LastAccessTime; }
            set { _fileInfo.LastWriteTime = value; }
        }

        public FileStreamBase OpenRead()
        {
            return new Net45FileStream(_fileInfo.OpenRead());
        }

        public static Net45FileInfo FromSystemFileInfo(FileInfo fileInfo)
        {
            return new Net45FileInfo(fileInfo);
        }
    }
}
