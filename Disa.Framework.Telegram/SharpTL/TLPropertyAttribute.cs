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
		/// Initializes a new instance of the <see cref="SharpTL.TLPropertyAttribute"/> class.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="flag">Flag.</param>
		public TLPropertyAttribute(uint order, uint flag, bool isFlag) 
		{
			Order = order;
			Flag = flag;
			IsFlag = isFlag;
		}


		/// <summary>
		///     Initializes a new instance of the <see cref="TLPropertyAttribute" /> class.
		/// </summary>
		/// <param name="order">Order in constructor.</param>
		/// <param name="serializationModeOverride">Serialization mode override.</param>
		public TLPropertyAttribute(uint order,uint flag, bool isFlag, TLSerializationMode serializationModeOverride)
		{
			Order = order;
			Flag = flag;
			IsFlag = true;
			SerializationModeOverride = serializationModeOverride;
		}


        /// <summary>
        ///     Order in constructor.
        /// </summary>
        public uint Order { get; set; }

		/// <summary>
		///     Flag order if its a flag
		/// </summary>
		public uint Flag { get; set; }

		/// <summary>
		///     Indicates if it is a flag
		/// </summary>
		public bool IsFlag { get; set; }

        /// <summary>
        ///     Serialization mode override.
        /// </summary>
        public TLSerializationMode? SerializationModeOverride { get; private set; }
    }
}
