// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TcpTransportPacket.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using SharpTL;

namespace SharpMTProto.Transport
{
    /// <summary>
    ///     TCP transport packet.
    /// </summary>
    /// <remarks>
    ///     Prepend payload with packet length (4 bytes) and sequential number (4 bytes),
    ///     append CRC32 (4 bytes) of the whole packet data bytes (except CRC32).
    /// </remarks>
    public class TcpTransportPacket
    {
        private const int PacketEmbracesLength = 12;

        private readonly byte[] _data;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpTransportPacket" /> class.
        /// </summary>
        /// <param name="data">Raw packet data bytes.</param>
        public TcpTransportPacket(byte[] data) : this(data, 0, data.Length)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpTransportPacket" /> class.
        /// </summary>
        /// <param name="buffer">Buffer with raw packet data bytes.</param>
        public TcpTransportPacket(ArraySegment<byte> buffer) : this(buffer.Array, buffer.Offset, buffer.Count)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpTransportPacket" /> class.
        /// </summary>
        /// <param name="buffer">Buffer with raw packet data bytes.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="length">Length.</param>
        public TcpTransportPacket(byte[] buffer, int offset, int length)
        {
            _data = new byte[length];
            Buffer.BlockCopy(buffer, offset, _data, 0, length);

            InitAndCheckConsistency();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpTransportPacket" /> class.
        /// </summary>
        /// <param name="number">Sequential packet number.</param>
        /// <param name="payload">Payload bytes.</param>
        public TcpTransportPacket(int number, byte[] payload)
        {
            Number = number;
            int length = payload.Length + PacketEmbracesLength;
            _data = new byte[length];
            using (var streamer = new TLStreamer(_data))
            {
                streamer.WriteInt32(length);
                streamer.WriteInt32(Number);
                streamer.Write(payload);
                Crc32 = ComputeCrc32();
                streamer.WriteUInt32(Crc32);
            }
        }

        /// <summary>
        ///     Raw data of the packet.
        /// </summary>
        public byte[] Data
        {
            get { return _data; }
        }

        /// <summary>
        ///     Length of the packet.
        /// </summary>
        public int Length
        {
            get { return _data.Length; }
        }

        /// <summary>
        ///     Length of a payload.
        /// </summary>
        public int PayloadLength
        {
            get { return Length - PacketEmbracesLength; }
        }

        /// <summary>
        ///     The sequential number of a TCP transport packet.
        /// </summary>
        public int Number { get; private set; }

        public uint Crc32 { get; private set; }

        private uint ComputeCrc32()
        {
            return BigMath.Utils.Crc32.Compute(_data, 0, _data.Length - 4);
        }

        private void InitAndCheckConsistency()
        {
            int length = _data.Length;
            using (var streamer = new TLStreamer(_data))
            {
                int expectedLength = streamer.ReadInt32();
                if (length != expectedLength)
                {
                    throw new TransportException(string.Format("Invalid packet length. Expected: {0}, actual: {1}.", expectedLength, length));
                }
                Number = streamer.ReadInt32();
                streamer.Seek(-4, SeekOrigin.End);
                Crc32 = streamer.ReadUInt32();
            }

            uint actualCrc32 = ComputeCrc32();
            if (Crc32 != actualCrc32)
            {
                throw new TransportException(string.Format("Invalid packet CRC32. Expected: {0}, actual: {1}.", actualCrc32, Crc32));
            }
        }

        /// <summary>
        ///     Try load TCP transport packet from a buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <param name="packet">Packet.</param>
        /// <returns>True - success, False - fail.</returns>
        public static bool TryLoad(ArraySegment<byte> buffer, out TcpTransportPacket packet)
        {
            packet = null;
            try
            {
                packet = new TcpTransportPacket(buffer);
                return true;
            }
            catch (TransportException e)
            {
                Console.WriteLine(e);
            }

            return false;
        }

        /// <summary>
        ///     Get a payload copied to a newly created array.
        /// </summary>
        /// <returns>Payload array of bytes.</returns>
        public byte[] GetPayloadCopy()
        {
            var buffer = new byte[PayloadLength];
            GetPayloadCopy(buffer, 0);
            return buffer;
        }

        /// <summary>
        ///     Get a payload copied to a buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <param name="offset">Offset.</param>
        public void GetPayloadCopy(byte[] buffer, int offset)
        {
            Buffer.BlockCopy(_data, 8, buffer, offset, PayloadLength);
        }
    }
}
