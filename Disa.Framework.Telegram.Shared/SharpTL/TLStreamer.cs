// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLStreamer.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using BigMath;
using BigMath.Utils;
using SharpTL.Annotations;

namespace SharpTL
{
    /// <summary>
    ///     TL streamer.
    /// </summary>
    public class TLStreamer : Stream
    {
        private const int BufferLength = 32;
        private static readonly byte[] ZeroBuffer = new byte[3];
        private readonly byte[] _buffer = new byte[BufferLength];
        private readonly bool _leaveOpen;
        private readonly Stack<long> _positionStack = new Stack<long>();
        private bool _disposed;
        private Stream _stream;
        private bool _streamAsLittleEndianInternal = true;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TLStreamer" /> class with underlying <see cref="MemoryStream" />.
        /// </summary>
        public TLStreamer() : this(new MemoryStream())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TLStreamer" /> class with underlying <see cref="MemoryStream" /> with
        ///     an expandable capacity initialized as specified.
        /// </summary>
        /// <param name="capacity">The initial size of the internal <see cref="MemoryStream" /> array in bytes.</param>
        public TLStreamer(int capacity) : this(new MemoryStream(capacity))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TLStreamer" /> class.
        /// </summary>
        /// <param name="bytes">Bytes.</param>
        public TLStreamer(byte[] bytes) : this(new MemoryStream(bytes))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TLStreamer" /> class.
        /// </summary>
        /// <param name="bytes">Bytes.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="count">Length from offset.</param>
        public TLStreamer(byte[] bytes, int offset, int count)
            : this(new MemoryStream(bytes, offset, count))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TLStreamer" /> class.
        /// </summary>
        /// <param name="bytes">Bytes as array segment.</param>
        public TLStreamer(ArraySegment<byte> bytes)
            : this(new MemoryStream(bytes.Array, bytes.Offset, bytes.Count))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TLStreamer" /> class.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="leaveOpen">Leave underlying stream open.</param>
        public TLStreamer([NotNull] Stream stream, bool leaveOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        /// <summary>
        ///     Underlying stream.
        /// </summary>
        public Stream BaseStream
        {
            get { return _stream; }
        }

        /// <summary>
        ///     Stream as little-endian.
        /// </summary>
        public bool StreamAsLittleEndian
        {
            get { return _streamAsLittleEndianInternal; }
            set { _streamAsLittleEndianInternal = value; }
        }

        /// <summary>
        ///     Current position.
        /// </summary>
        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        /// <summary>
        ///     Bytes till end.
        /// </summary>
        public virtual long BytesTillEnd
        {
            get { return Length - Position; }
        }

        /// <summary>
        ///     Sets a value indicating whether the underlying stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        /// <summary>
        ///     Length.
        /// </summary>
        public override long Length
        {
            get { return _stream.Length; }
        }

        /// <summary>
        ///     Gets a value indicating whether the underlying stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        /// <summary>
        ///     Gets a value indicating whether the underlying stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        /// <summary>
        ///     Create syncronized wrapper around the <see cref="TLStreamer" />.
        /// </summary>
        /// <returns>Syncronized wrapper.</returns>
        public TLStreamer Syncronized()
        {
            return Syncronized(this);
        }

        /// <summary>
        ///     Create syncronized wrapper around the <see cref="TLStreamer" />.
        /// </summary>
        /// <param name="streamer">TL streamer.</param>
        /// <returns>Syncronized wrapper.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static TLStreamer Syncronized([NotNull] TLStreamer streamer)
        {
            if (streamer == null)
            {
                throw new ArgumentNullException("streamer");
            }

            return new TLSyncStreamer(streamer);
        }

        /// <summary>
        ///     Reads bytes to a buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="count">Count.</param>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        /// <summary>
        ///     Sets the length of the underlying stream.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        /// <summary>
        ///     Writes bytes from a buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="count">Count.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        /// <summary>
        ///     Writes all bytes from a buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        public virtual void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        ///     Writes all bytes from an array segment.
        /// </summary>
        /// <param name="buffer">Array segment.</param>
        public virtual void Write(ArraySegment<byte> buffer)
        {
            Write(buffer.Array, buffer.Offset, buffer.Count);
        }

        /// <summary>
        ///     Reads an array of bytes.
        /// </summary>
        /// <param name="count">Count to read.</param>
        /// <returns>Array of bytes.</returns>
        public virtual byte[] ReadBytes(int count)
        {
            var buffer = new byte[count];
            Read(buffer, 0, count);
            return buffer;
        }

        /// <summary>
        ///     Reads byte.
        /// </summary>
        public override int ReadByte()
        {
            return _stream.ReadByte();
        }

        /// <summary>
        ///     Sets the position within the underlying stream.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        /// <summary>
        ///     Writes a byte to the current position in the stream and advances the position within the stream by one byte.
        /// </summary>
        public override void WriteByte(byte value)
        {
            _buffer[0] = value;
            Write(_buffer, 0, 1);
        }

        /// <summary>
        ///     Reads 32-bit signed integer.
        /// </summary>
        public virtual int ReadInt32()
        {
            FillBuffer(4);
            return _buffer.ToInt32(0, _streamAsLittleEndianInternal);
        }

        /// <summary>
        ///     Writes 32-bit signed integer.
        /// </summary>
        public virtual void WriteInt32(int value)
        {
            value.ToBytes(_buffer, 0, _streamAsLittleEndianInternal);
            Write(_buffer, 0, 4);
        }

        /// <summary>
        ///     Reads 32-bit unsigned integer.
        /// </summary>
        public virtual uint ReadUInt32()
        {
            return (uint) ReadInt32();
        }

        /// <summary>
        ///     Writes 32-bit unsigned integer.
        /// </summary>
        public virtual void WriteUInt32(uint value)
        {
            WriteInt32((int) value);
        }

        /// <summary>
        ///     Reads 64-bit signed integer.
        /// </summary>
        public virtual long ReadInt64()
        {
            FillBuffer(8);
            return _buffer.ToInt64(0, _streamAsLittleEndianInternal);
        }

        /// <summary>
        ///     Writes 64-bit signed integer.
        /// </summary>
        public virtual void WriteInt64(long value)
        {
            value.ToBytes(_buffer, 0, _streamAsLittleEndianInternal);
            Write(_buffer, 0, 8);
        }

        /// <summary>
        ///     Reads 64-bit unsigned integer.
        /// </summary>
        public virtual ulong ReadUInt64()
        {
            return (ulong) ReadInt64();
        }

        /// <summary>
        ///     Writes 64-bit unsigned integer.
        /// </summary>
        public virtual void WriteUInt64(ulong value)
        {
            WriteInt64((long) value);
        }

        /// <summary>
        ///     Reads double.
        /// </summary>
        public virtual double ReadDouble()
        {
            return BitConverter.Int64BitsToDouble(ReadInt64());
        }

        /// <summary>
        ///     Writes double.
        /// </summary>
        public virtual void WriteDouble(double value)
        {
            WriteInt64(BitConverter.DoubleToInt64Bits(value));
        }

        /// <summary>
        ///     Reads a 128-bit signed integer.
        /// </summary>
        public virtual Int128 ReadInt128()
        {
            FillBuffer(16);
            return _buffer.ToInt128(0, _streamAsLittleEndianInternal);
        }

        /// <summary>
        ///     Writes a 128-bit signed integer.
        /// </summary>
        public virtual void WriteInt128(Int128 value)
        {
            value.ToBytes(_buffer, 0, _streamAsLittleEndianInternal);
            Write(_buffer, 0, 16);
        }

        /// <summary>
        ///     Reads a 256-bit signed integer.
        /// </summary>
        public virtual Int256 ReadInt256()
        {
            FillBuffer(32);
            return _buffer.ToInt256(0, _streamAsLittleEndianInternal);
        }

        /// <summary>
        ///     Writes a 256-bit signed integer.
        /// </summary>
        public virtual void WriteInt256(Int256 value)
        {
            value.ToBytes(_buffer, 0, _streamAsLittleEndianInternal);
            Write(_buffer, 0, 32);
        }

        /// <summary>
        ///     Reads a bunch of bytes formated as described in TL.
        /// </summary>
        public virtual byte[] ReadTLBytes()
        {
            int offset = 1;
            int length = ReadByte();
            if (length == 254)
            {
                offset = 4;
                length = ReadByte() | ReadByte() << 8 | ReadByte() << 16;
            }
            var bytes = new byte[length];
            Read(bytes, 0, length);

            offset = 4 - (offset + length)%4;
            if (offset < 4)
            {
                Position += offset;
            }
            return bytes;
        }

        /// <summary>
        ///     Writes a bunch of bytes formated as described in TL.
        /// </summary>
        /// <param name="bytes">Array of bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException">When array size exceeds </exception>
        public virtual void WriteTLBytes(byte[] bytes)
        {
            int length = bytes.Length;
            int offset = 1;
            if (length <= 253)
            {
                WriteByte((byte) length);
            }
            else if (length >= 254 && length <= 0xFFFFFF)
            {
                offset = 4;
                var lBytes = new byte[4];
                lBytes[0] = 254;
                lBytes[1] = (byte) length;
                lBytes[2] = (byte) (length >> 8);
                lBytes[3] = (byte) (length >> 16);
                Write(lBytes, 0, 4);
            }
            else
            {
                throw new ArgumentOutOfRangeException(string.Format("Array length {0} exceeds the maximum allowed {1}.", length, 0xFFFFFF));
            }

            Write(bytes, 0, length);

            offset = 4 - (offset + length)%4;
            if (offset < 4)
            {
                Write(ZeroBuffer, 0, offset);
            }
        }

        /// <summary>
        ///     Writes random data till the end of an underlying stream.
        /// </summary>
        public void WriteRandomDataTillEnd()
        {
            WriteRandomData((int) (Length - Position));
        }

        /// <summary>
        ///     Writes random data of an underlying stream.
        /// </summary>
        /// <param name="length">Length of the data to write.</param>
        public void WriteRandomData(int length)
        {
            if (length <= 0)
            {
                return;
            }
            if ((Length - Position) < length)
            {
                throw new InvalidOperationException("Length of a random data must be less of equal to underlying stream length minus current position.");
            }

            var buffer = new byte[length];
            var rnd = new Random();
            rnd.NextBytes(buffer);
            Write(buffer);
        }

        /// <summary>
        ///     Clears all buffers for the underlying stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            _stream.Flush();
        }

        /// <summary>
        ///     Push current position to a stack.
        /// </summary>
        /// <returns>Current position.</returns>
        public long PushPosition()
        {
            var position = Position;
            _positionStack.Push(position);
            return position;
        }

        /// <summary>
        ///     Pop current position from a stack.
        /// </summary>
        /// <returns>Current position.</returns>
        public long PopPosition()
        {
            var position = _positionStack.Pop();
            Position = position;
            return position;
        }

        /// <summary>
        ///     Fills the internal buffer with the specified number of bytes read from the stream.
        /// </summary>
        /// <param name="numBytes">The number of bytes to be read. </param>
        /// <exception cref="T:System.IO.EndOfStreamException">
        ///     The end of the stream is reached before <paramref name="numBytes" />
        ///     could be read.
        /// </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     Requested <paramref name="numBytes" /> is larger than the
        ///     internal buffer size.
        /// </exception>
        protected virtual void FillBuffer(int numBytes)
        {
            if (numBytes < 0 || numBytes > BufferLength)
            {
                throw new ArgumentOutOfRangeException("numBytes");
            }

            int offset = 0;
            if (numBytes == 1)
            {
                int num = _stream.ReadByte();
                if (num == -1)
                {
                    throw new EndOfStreamException();
                }
                _buffer[0] = (byte) num;
            }
            else
            {
                do
                {
                    int num = _stream.Read(_buffer, offset, numBytes - offset);
                    if (num == 0)
                    {
                        throw new EndOfStreamException();
                    }
                    offset += num;
                } while (offset < numBytes);
            }
        }

        #region Disposing
        /// <summary>
        ///     Dispose.
        /// </summary>
        /// <param name="disposing">
        ///     A call to Dispose(false) should only clean up native resources. A call to Dispose(true) should clean up both
        ///     managed and native resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_stream != null)
                {
                    if (_leaveOpen)
                    {
                        _stream.Flush();
                    }
                    else
                    {
                        _stream.Dispose();
                    }
                    _stream = null;
                }
            }

            _disposed = true;
        }
        #endregion

        /// <summary>
        ///     Thread-safe TL streamer.
        /// </summary>
        private sealed class TLSyncStreamer : TLStreamer
        {
            private readonly TLStreamer _streamer;

            internal TLSyncStreamer(TLStreamer streamer)
            {
                if (streamer == null)
                {
                    throw new ArgumentNullException("streamer");
                }
                _streamer = streamer;
            }

            public override bool CanRead
            {
                get { return _streamer.CanRead; }
            }

            public override bool CanWrite
            {
                get { return _streamer.CanWrite; }
            }

            public override bool CanSeek
            {
                get { return _streamer.CanSeek; }
            }

            [ComVisible(false)]
            public override bool CanTimeout
            {
                get { return _streamer.CanTimeout; }
            }

            public override long Length
            {
                get
                {
                    lock (_streamer)
                        return _streamer.Length;
                }
            }

            public override long Position
            {
                get
                {
                    lock (_streamer)
                        return _streamer.Position;
                }
                set
                {
                    lock (_streamer)
                        _streamer.Position = value;
                }
            }

            [ComVisible(false)]
            public override int ReadTimeout
            {
                get { return _streamer.ReadTimeout; }
                set { _streamer.ReadTimeout = value; }
            }

            [ComVisible(false)]
            public override int WriteTimeout
            {
                get { return _streamer.WriteTimeout; }
                set { _streamer.WriteTimeout = value; }
            }

            protected override void Dispose(bool disposing)
            {
                lock (_streamer)
                {
                    try
                    {
                        if (!disposing)
                        {
                            return;
                        }
                        _streamer.Dispose();
                    }
                    finally
                    {
                        base.Dispose(disposing);
                    }
                }
            }

            public override void Flush()
            {
                lock (_streamer)
                    _streamer.Flush();
            }

            public override int Read(byte[] bytes, int offset, int count)
            {
                lock (_streamer)
                    return _streamer.Read(bytes, offset, count);
            }

            public override int ReadByte()
            {
                lock (_streamer)
                    return _streamer.ReadByte();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                lock (_streamer)
                    return _streamer.Seek(offset, origin);
            }

            public override void SetLength(long length)
            {
                lock (_streamer)
                    _streamer.SetLength(length);
            }

            public override void Write(byte[] bytes, int offset, int count)
            {
                lock (_streamer)
                    _streamer.Write(bytes, offset, count);
            }

            public override void WriteByte(byte b)
            {
                lock (_streamer)
                    _streamer.WriteByte(b);
            }

            public override void Write(byte[] buffer)
            {
                lock (_streamer)
                    _streamer.Write(buffer);
            }

            public override byte[] ReadBytes(int count)
            {
                lock (_streamer)
                    return _streamer.ReadBytes(count);
            }

            public override int ReadInt32()
            {
                lock (_streamer)
                    return _streamer.ReadInt32();
            }

            public override void WriteInt32(int value)
            {
                lock (_streamer)
                    _streamer.WriteInt32(value);
            }

            public override uint ReadUInt32()
            {
                lock (_streamer)
                    return _streamer.ReadUInt32();
            }

            public override void WriteUInt32(uint value)
            {
                lock (_streamer)
                    _streamer.WriteUInt32(value);
            }

            public override long ReadInt64()
            {
                lock (_streamer)
                    return _streamer.ReadInt64();
            }

            public override void WriteInt64(long value)
            {
                lock (_streamer)
                    _streamer.WriteInt64(value);
            }

            public override ulong ReadUInt64()
            {
                lock (_streamer)
                    return _streamer.ReadUInt64();
            }

            public override void WriteUInt64(ulong value)
            {
                lock (_streamer)
                    _streamer.WriteUInt64(value);
            }

            public override double ReadDouble()
            {
                lock (_streamer)
                    return _streamer.ReadDouble();
            }

            public override void WriteDouble(double value)
            {
                lock (_streamer)
                    _streamer.WriteDouble(value);
            }

            public override Int128 ReadInt128()
            {
                lock (_streamer)
                    return _streamer.ReadInt128();
            }

            public override void WriteInt128(Int128 value)
            {
                lock (_streamer)
                    _streamer.WriteInt128(value);
            }

            public override Int256 ReadInt256()
            {
                lock (_streamer)
                    return _streamer.ReadInt256();
            }

            public override void WriteInt256(Int256 value)
            {
                lock (_streamer)
                    _streamer.WriteInt256(value);
            }

            public override byte[] ReadTLBytes()
            {
                lock (_streamer)
                    return _streamer.ReadTLBytes();
            }

            public override void WriteTLBytes(byte[] bytes)
            {
                lock (_streamer)
                    _streamer.WriteTLBytes(bytes);
            }
        }
    }
}
