// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RNGCryptoServiceProviderBase.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Raksha.Security.Cryptography
{
    public abstract class RNGCryptoServiceProviderBase : IDisposable
    {
        public abstract void GetBytes(byte[] data);

        public static RNGCryptoServiceProviderBase Create()
        {
            return new Net45RNGCryptoServiceProvider();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
