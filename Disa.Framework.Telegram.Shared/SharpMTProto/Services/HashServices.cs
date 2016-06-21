// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HashServices.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#if !OVERRIDE_PCL

using System;
using System.IO;
using Raksha.Crypto.Digests;

namespace SharpMTProto.Services
{
    public class HashServices : IHashServices
    {
        public byte[] ComputeSHA1(byte[] data)
        {
            return ComputeSHA1(data, 0, data.Length);
        }

        public byte[] ComputeSHA1(byte[] data, int offset, int count)
        {
            var digest = new Sha1Digest();

            digest.BlockUpdate(data, offset, count);

            var output = new byte[20];
            digest.DoFinal(output, 0);
            return output;
        }

        public byte[] ComputeSHA1(ArraySegment<byte> data)
        {
            return ComputeSHA1(data.Array, data.Offset, data.Count);
        }

        public byte[] ComputeSHA1(Stream stream)
        {
            var digest = new Sha1Digest();

            var buffer = new byte[256];
            int read;
            while ((read = stream.Read(buffer, 0, 256)) > 0)
            {
                digest.BlockUpdate(buffer, 0, read);
            }

            var output = new byte[20];
            digest.DoFinal(output, 0);
            return output;
        }
    }
}

#endif
