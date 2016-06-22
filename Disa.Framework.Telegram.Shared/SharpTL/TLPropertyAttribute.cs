// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLPropertyAttribute.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpTL
{
    /// <summary>
    ///     TL property attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TLPropertyAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TLPropertyAttribute" /> class.
        /// </summary>
        /// <param name="order">Order in constructor.</param>
        public TLPropertyAttribute(uint order)
        {
            Order = order;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TLPropertyAttribute" /> class.
        /// </summary>
        /// <param name="order">Order in constructor.</param>
        /// <param name="serializationModeOverride">Serialization mode override.</param>
        public TLPropertyAttribute(uint order, TLSerializationMode serializationModeOverride)
        {
            Order = order;
            SerializationModeOverride = serializationModeOverride;
        }

        /// <summary>
        ///     Order in constructor.
        /// </summary>
        public uint Order { get; set; }

        /// <summary>
        ///     Serialization mode override.
        /// </summary>
        public TLSerializationMode? SerializationModeOverride { get; private set; }
    }
}
