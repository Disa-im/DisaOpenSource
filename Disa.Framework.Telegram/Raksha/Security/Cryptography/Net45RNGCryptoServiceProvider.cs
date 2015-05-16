// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Net45RNGCryptoServiceProvider.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Raksha.Security.Cryptography
{
    public class Net45RNGCryptoServiceProvider : RNGCryptoServiceProviderBase
    {
        private readonly System.Security.Cryptography.RNGCryptoServiceProvider _internalRNGCSP;

        public Net45RNGCryptoServiceProvider()
        {
            _internalRNGCSP = new System.Security.Cryptography.RNGCryptoServiceProvider();
        }

        public override void GetBytes(byte[] data)
        {
            _internalRNGCSP.GetBytes(data);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing)
                {
                    return;
                }

                _internalRNGCSP.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
