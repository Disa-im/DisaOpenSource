// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricKeyEntry.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using Raksha.Crypto;
using Raksha.Utilities;

namespace Raksha.Pkcs
{
    public class AsymmetricKeyEntry : Pkcs12Entry
    {
        private readonly AsymmetricKeyParameter _key;

        public AsymmetricKeyEntry(AsymmetricKeyParameter key) : base(Platform.CreateHashtable())
        {
            _key = key;
        }

        public AsymmetricKeyEntry(AsymmetricKeyParameter key, IDictionary attributes) : base(attributes)
        {
            _key = key;
        }

        public AsymmetricKeyParameter Key
        {
            get { return _key; }
        }

        public override bool Equals(object obj)
        {
            var other = obj as AsymmetricKeyEntry;

            if (other == null)
            {
                return false;
            }

            return _key.Equals(other._key);
        }

        public override int GetHashCode()
        {
            return ~_key.GetHashCode();
        }
    }
}
