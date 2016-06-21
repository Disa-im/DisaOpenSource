// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Enums.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#if PCL

using System;

namespace Raksha.IO
{
    [Flags]
    public enum FileAccess
    {
        Read = 1,
        Write = 2,
        ReadWrite = 3
    }

    public enum FileMode
    {
        CreateNew = 1,
        Create = 2,
        Open = 3,
        OpenOrCreate = 4,
        Truncate = 5,
        Append = 6
    }

    [Flags]
    public enum FileShare
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = 3,
        Delete = 4,
        Inheritable = 16
    }
}
#endif