// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RandomGenerator.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Reactive.Linq;

namespace SharpMTProto.Services
{
    public interface IRandomGenerator
    {
        void FillWithRandom(ArraySegment<byte> bytes);
        void FillWithRandom(byte[] buffer);
    }

    public class RandomGenerator : IRandomGenerator
    {
        private const int BufferLength = 32;
        private readonly byte[] _buffer = new byte[BufferLength];
        private readonly Random _random;

        public RandomGenerator()
        {
            _random = new Random();
        }

        public RandomGenerator(int seed)
        {
            _random = new Random(seed);
        }

        public void FillWithRandom(ArraySegment<byte> bytes)
        {
            if (bytes.Array == null)
                throw new ArgumentNullException("bytes", "Array is null.");
            if (bytes.Offset + bytes.Count > bytes.Array.Length)
                throw new ArgumentOutOfRangeException("bytes", "Array length must be greater or equal to (offset + count).");

            int count = bytes.Count;
            int offset = bytes.Offset;
            while (count > 0)
            {
                _random.NextBytes(_buffer);
                int bytesToWrite = count > BufferLength ? BufferLength : count;
                Buffer.BlockCopy(_buffer, 0, bytes.Array, offset, bytesToWrite);
                count -= bytesToWrite;
                offset += bytesToWrite;
            }
        }

        public void FillWithRandom(byte[] buffer)
        {
            FillWithRandom(new ArraySegment<byte>(buffer, 0, buffer.Length));
        }
    }
}
