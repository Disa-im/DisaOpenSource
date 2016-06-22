// --------------------------------------------------------------------------------------------------------------------
// <copyright file="X509CollectionStore.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using Raksha.Utilities;

namespace Raksha.X509.Store
{
    /// <summary>
    ///     A simple collection backed store.
    /// </summary>
    internal class X509CollectionStore : IX509Store
    {
        private readonly ICollection _local;

        /**
		 * Basic constructor.
		 *
		 * @param collection - initial contents for the store, this is copied.
		 */

        internal X509CollectionStore(ICollection collection)
        {
            _local = Platform.CreateArrayList(collection);
        }

        /**
		 * Return the matches in the collection for the passed in selector.
		 *
		 * @param selector the selector to match against.
		 * @return a possibly empty collection of matching objects.
		 */

        public ICollection GetMatches(IX509Selector selector)
        {
            if (selector == null)
            {
                return Platform.CreateArrayList(_local);
            }

            IList result = Platform.CreateArrayList();
            foreach (object obj in _local)
            {
                if (selector.Match(obj))
                {
                    result.Add(obj);
                }
            }

            return result;
        }
    }
}
