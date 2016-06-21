// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLVectorAttribute.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpTL
{
    /// <summary>
    ///     TL vector attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TLVectorAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TLVectorAttribute" /> class.
        /// </summary>
        /// <param name="itemsModeOverride">Vector items serialization mode override.</param>
        public TLVectorAttribute(TLSerializationMode itemsModeOverride)
        {
            ItemsModeOverride = itemsModeOverride;
        }

        /// <summary>
        ///     Vector items serialization mode override.
        /// </summary>
        public TLSerializationMode ItemsModeOverride { get; private set; }
    }
}
