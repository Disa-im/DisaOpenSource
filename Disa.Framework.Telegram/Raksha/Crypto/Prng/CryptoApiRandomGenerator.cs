// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoApiRandomGenerator.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Raksha.Security.Cryptography;

namespace Raksha.Crypto.Prng
{
    /// <summary>
    ///     Uses current platform's RNGCryptoServiceProvider.
    /// </summary>
    public class CryptoApiRandomGenerator : IRandomGenerator, IDisposable
    {
        private RNGCryptoServiceProviderBase _rndProv;

        public CryptoApiRandomGenerator()
        {
            _rndProv = RNGCryptoServiceProviderBase.Create();
        }

        #region IRandomGenerator Members
        public virtual void AddSeedMaterial(byte[] seed)
        {
            // We don't care about the seed
        }

        public virtual void AddSeedMaterial(long seed)
        {
            // We don't care about the seed
        }

        public virtual void NextBytes(byte[] bytes)
        {
            _rndProv.GetBytes(bytes);
        }

        public virtual void NextBytes(byte[] bytes, int start, int len)
        {
            if (start < 0)
            {
                throw new ArgumentException("Start offset cannot be negative", "start");
            }
            if (bytes.Length < (start + len))
            {
                throw new ArgumentException("Byte array too small for requested offset and length");
            }

            if (bytes.Length == len && start == 0)
            {
                NextBytes(bytes);
            }
            else
            {
                var tmpBuf = new byte[len];
                _rndProv.GetBytes(tmpBuf);
                Array.Copy(tmpBuf, 0, bytes, start, len);
            }
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (_rndProv != null)
            {
                _rndProv.Dispose();
                _rndProv = null;
            }
        }
    }
}
