//////////////////////////////////////////////////////////
// Copyright (c) Alexander Logger. All rights reserved. //
//////////////////////////////////////////////////////////

namespace SharpTL
{
    using System;
    using System.IO;

    /// <summary>
    ///     Encapsulates a <see cref="System.IO.Stream" /> to calculate the CRC32 checksum on-the-fly as data passes through.
    /// </summary>
    public class TLCrcStreamer : TLStreamer
    {
        private uint _readCrc = unchecked(0xFFFFFFFF);
        private uint _writeCrc = unchecked(0xFFFFFFFF);

        public TLCrcStreamer()
        {
        }

        public TLCrcStreamer(int capacity) : base(capacity)
        {
        }

        public TLCrcStreamer(byte[] bytes) : base(bytes)
        {
        }

        public TLCrcStreamer(byte[] bytes, int offset, int count) : base(bytes, offset, count)
        {
        }

        public TLCrcStreamer(ArraySegment<byte> bytes) : base(bytes)
        {
        }

        public TLCrcStreamer(Stream stream, bool leaveOpen = false) : base(stream, leaveOpen)
        {
        }

        /// <summary>
        ///     Gets the CRC checksum of the data that was read by the stream thus far.
        /// </summary>
        public uint ReadCrc
        {
            get { return unchecked(_readCrc ^ 0xFFFFFFFF); }
        }

        /// <summary>
        ///     Gets the CRC checksum of the data that was written to the stream thus far.
        /// </summary>
        public uint WriteCrc
        {
            get { return unchecked(_writeCrc ^ 0xFFFFFFFF); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = base.Read(buffer, offset, count);
            _readCrc = CalculateCrc(_readCrc, buffer, offset, count);
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);
            _writeCrc = CalculateCrc(_writeCrc, buffer, offset, count);
        }

        private uint CalculateCrc(uint crc, byte[] buffer, int offset, int count)
        {
            unchecked
            {
                for (int i = offset, end = offset + count; i < end; i++)
                    crc = (crc >> 8) ^ Table[(crc ^ buffer[i]) & 0xFF];
            }
            return crc;
        }

        private static uint[] GenerateTable()
        {
            unchecked
            {
                var table = new uint[256];

                const uint poly = 0xEDB88320;
                for (uint i = 0; i < table.Length; i++)
                {
                    var crc = i;
                    for (var j = 8; j > 0; j--)
                    {
                        if ((crc & 1) == 1)
                            crc = (crc >> 1) ^ poly;
                        else
                            crc >>= 1;
                    }
                    table[i] = crc;
                }

                return table;
            }
        }

        /// <summary>
        ///     Resets the read and write checksums.
        /// </summary>
        public void ResetChecksum()
        {
            _readCrc = unchecked(0xFFFFFFFF);
            _writeCrc = unchecked(0xFFFFFFFF);
        }

        private static readonly uint[] Table = GenerateTable();
    }
}
