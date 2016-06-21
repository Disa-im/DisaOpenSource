// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionConfig.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace SharpMTProto
{
    /// <summary>
    ///     Connection config.
    /// </summary>
    public class ConnectionConfig
    {
        private readonly byte[] _authKey;
        private ulong _salt;
        private ulong _sessionId;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionConfig" /> class.
        /// </summary>
        /// <param name="authKey">Auth key.</param>
        /// <param name="salt">Salt.</param>
        /// <exception cref="System.ArgumentException">The <paramref name="authKey" /> is <c>null</c> or an empty array.</exception>
        public ConnectionConfig(byte[] authKey, ulong salt)
        {
            _authKey = authKey;
            _salt = salt;
        }

        /// <summary>
        ///     Auth key.
        /// </summary>
        public byte[] AuthKey
        {
            get { return _authKey; }
        }

        /// <summary>
        ///     Salt.
        /// </summary>
        public ulong Salt
        {
            get { return _salt; }
            set
            {
                _salt = value;
                Save();
            }
        }

        public ulong SessionId
        {
            get { return _sessionId; }
            set
            {
                _sessionId = value;
                Save();
            }
        }

        /// <summary>
        ///     Save config.
        /// </summary>
        protected virtual void Save()
        {
        }
    }
}
