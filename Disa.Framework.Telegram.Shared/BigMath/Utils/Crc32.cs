// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Crc32.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace BigMath.Utils
{
    /// <summary>
    ///     Crc32 calculator.
    /// </summary>
    public class Crc32
    {
        private const uint KCrcPoly = 0xEDB88320;
        private const uint KInitial = 0xFFFFFFFF;
        private const uint CrcNumTables = 8;
        private static readonly uint[] Table;

        private uint _value;

        static Crc32()
        {
            unchecked
            {
                Table = new uint[256*CrcNumTables];
                uint i;
                for (i = 0; i < 256; i++)
                {
                    uint r = i;
                    for (int j = 0; j < 8; j++)
                    {
                        r = (r >> 1) ^ (KCrcPoly & ~((r & 1) - 1));
                    }
                    Table[i] = r;
                }
                for (; i < 256*CrcNumTables; i++)
                {
                    uint r = Table[i - 256];
                    Table[i] = Table[r & 0xFF] ^ (r >> 8);
                }
            }
        }

        public Crc32()
        {
            Init();
        }

        public uint Value
        {
            get { return ~_value; }
        }

        /// <summary>
        ///     Reset CRC.
        /// </summary>
        public void Init()
        {
            _value = KInitial;
        }

        public void UpdateByte(byte b)
        {
            _value = (_value >> 8) ^ Table[(byte) _value ^ b];
        }

        public void Update(byte[] data, int offset, int count)
        {
            new ArraySegment<byte>(data, offset, count); // check arguments
            if (count == 0)
            {
                return;
            }

            uint[] table = Table; // important for performance!

            uint crc = _value;

            for (; (offset & 7) != 0 && count != 0; count--)
            {
                crc = (crc >> 8) ^ table[(byte) crc ^ data[offset++]];
            }

            if (count >= 8)
            {
                /*
                 * Idea from 7-zip project sources (http://7-zip.org/sdk.html)
                 */

                int to = (count - 8) & ~7;
                count -= to;
                to += offset;

                while (offset != to)
                {
                    crc ^= (uint) (data[offset] + (data[offset + 1] << 8) + (data[offset + 2] << 16) + (data[offset + 3] << 24));
                    var high = (uint) (data[offset + 4] + (data[offset + 5] << 8) + (data[offset + 6] << 16) + (data[offset + 7] << 24));
                    offset += 8;

                    crc = table[(byte) crc + 0x700] ^ table[(byte) (crc >>= 8) + 0x600] ^ table[(byte) (crc >>= 8) + 0x500] ^ table[ /*(byte)*/(crc >> 8) + 0x400] ^
                        table[(byte) (high) + 0x300] ^ table[(byte) (high >>= 8) + 0x200] ^ table[(byte) (high >>= 8) + 0x100] ^ table[ /*(byte)*/(high >> 8) + 0x000];
                }
            }

            while (count-- != 0)
            {
                crc = (crc >> 8) ^ table[(byte) crc ^ data[offset++]];
            }

            _value = crc;
        }

        public static uint Compute(byte[] data, int offset, int size)
        {
            var crc = new Crc32();
            crc.Update(data, offset, size);
            return crc.Value;
        }

        public static uint Compute(byte[] data)
        {
            return Compute(data, 0, data.Length);
        }

        public static uint Compute(ArraySegment<byte> block)
        {
            return Compute(block.Array, block.Offset, block.Count);
        }

        public static uint Compute(string text)
        {
            return Compute(text, Encoding.UTF8);
        }

        public static uint Compute(string text, Encoding encoding)
        {
            return Compute(encoding.GetBytes(text));
        }
    }
}
