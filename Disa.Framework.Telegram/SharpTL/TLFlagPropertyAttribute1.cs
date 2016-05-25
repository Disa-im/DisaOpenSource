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
    public class TLFlagPropertyAttribute : Attribute
    {
		
        /// <summary>
        ///     Initializes a new instance of the <see cref="TLPropertyAttribute" /> class.
        /// </summary>
        /// <param name="order">Order in constructor.</param>
		public TLFlagPropertyAttribute(uint order,uint flag)
        {
            Order = order;
			Flag = flag;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TLPropertyAttribute" /> class.
        /// </summary>
        /// <param name="order">Order in constructor.</param>
        /// <param name="serializationModeOverride">Serialization mode override.</param>
		public TLFlagPropertyAttribute(uint order, uint flag, TLSerializationMode serializationModeOverride)
        {
            Order = order;
			Flag = flag;
            SerializationModeOverride = serializationModeOverride;
        }

        /// <summary>
        ///     Order in constructor.
        /// </summary>
        public uint Order { get; set; }

		/// <summary>
		/// Gets or sets the flag.
		/// </summary>
		/// <value>The flag.</value>
		public uint Flag{ get; set;}

        /// <summary>
        ///     Serialization mode override.
        /// </summary>
        public TLSerializationMode? SerializationModeOverride { get; private set; }
    }
}
