// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Raksha.IO
{
    public class Net45FileStream : FileStreamBase
    {
        private FileStream _platformFileStream;

        public Net45FileStream(FileStream fileStream)
        {
            _platformFileStream = fileStream;
        }

        public override bool CanRead
        {
            get { return _platformFileStream.CanRead; }
        }

        public override bool CanWrite
        {
            get { return _platformFileStream.CanWrite; }
        }

        public override bool CanSeek
        {
            get { return _platformFileStream.CanSeek; }
        }

        public override bool CanTimeout
        {
            get { return _platformFileStream.CanTimeout; }
        }

        public override long Length
        {
            get { return _platformFileStream.Length; }
        }

        public override long Position
        {
            get { return _platformFileStream.Position; }
            set { _platformFileStream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return _platformFileStream.ReadTimeout; }
            set { _platformFileStream.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return _platformFileStream.WriteTimeout; }
            set { _platformFileStream.WriteTimeout = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _platformFileStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _platformFileStream.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            _platformFileStream.Flush();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _platformFileStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _platformFileStream.EndRead(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _platformFileStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _platformFileStream.EndWrite(asyncResult);
        }

        public override int ReadByte()
        {
            return _platformFileStream.ReadByte();
        }

        public override void WriteByte(byte value)
        {
            _platformFileStream.WriteByte(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _platformFileStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _platformFileStream.SetLength(value);
        }

        public override void Close()
        {
            _platformFileStream.Close();
        }

        protected override void Dispose(bool disposing)
        {
            _platformFileStream.Dispose();
        }

        protected static FileMode ToInternalFileMode(FileMode fileMode)
        {
            return (FileMode) ((int) fileMode);
        }

        protected static FileAccess ToInternalFileAccess(FileAccess fileAccess)
        {
            return (FileAccess) ((int) fileAccess);
        }

        protected static FileShare ToInternalFileShare(FileShare fileShare)
        {
            return (FileShare) ((int) fileShare);
        }

        protected override void Initialize(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
        {
            _platformFileStream = new FileStream(path, ToInternalFileMode(mode), ToInternalFileAccess(access), ToInternalFileShare(share), bufferSize);
        }
    }
}
