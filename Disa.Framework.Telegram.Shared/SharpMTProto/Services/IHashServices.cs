// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHashServices.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace SharpMTProto.Services
{
    public interface IHashServices
    {
        byte[] ComputeSHA1(byte[] data);
        byte[] ComputeSHA1(byte[] data, int offset, int count);
        byte[] ComputeSHA1(ArraySegment<byte> data);
        byte[] ComputeSHA1(Stream stream);
    }
}
