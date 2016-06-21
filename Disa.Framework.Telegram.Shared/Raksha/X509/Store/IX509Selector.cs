// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IX509Selector.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Raksha.X509.Store
{
    public interface IX509Selector
    {
        object Clone();
        bool Match(object obj);
    }
}
