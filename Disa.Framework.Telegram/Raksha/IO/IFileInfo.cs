// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFileInfo.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Raksha.IO
{
    /// <summary>
    ///     Interface for a file info.
    /// </summary>
    public interface IFileInfo
    {
        /// <summary>
        ///     Full name.
        /// </summary>
        string FullName { get; }

        /// <summary>
        ///     Name.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Length.
        /// </summary>
        long Length { get; }

        /// <summary>
        ///     Last write time.
        /// </summary>
        DateTime LastWriteTime { get; set; }

        /// <summary>
        ///     Open read.
        /// </summary>
        /// <returns></returns>
        FileStreamBase OpenRead();
    }
}
