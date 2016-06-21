// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AuthInfo.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpMTProto.Authentication
{
    /// <summary>
    ///     Auth info contains of auth key and initial salt.
    /// </summary>
    public class AuthInfo
    {
        private readonly byte[] _authKey;
        private readonly UInt64 _salt;

        public AuthInfo(byte[] authKey, ulong salt)
        {
            _authKey = authKey;
            _salt = salt;
        }

        public byte[] AuthKey
        {
            get { return _authKey; }
        }

        public ulong Salt
        {
            get { return _salt; }
        }
    }
}
