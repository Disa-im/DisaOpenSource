// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLPropertyInfo.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Reflection;

namespace SharpTL
{
    /// <summary>
    ///     TL property info.
    /// </summary>
    public class TLPropertyInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TLPropertyInfo" /> class.
        /// </summary>
        /// <param name="order">Order in constructor.</param>
        /// <param name="propertyInfo">Property info.</param>
        /// <param name="serializationModeOverride">Serialization mode override.</param>
		public TLPropertyInfo(uint order, uint flag, bool isFlag, PropertyInfo propertyInfo, TLSerializationMode? serializationModeOverride = null)
        {
			//Console.WriteLine("##### new before TLProperty info propertyInfo  " +  propertyInfo + " flags " + isFlag);
            Order = order;
            PropertyInfo = propertyInfo;
			if (isFlag) {
				IsFlag = true;
				Flag = flag;
			}
            SerializationModeOverride = serializationModeOverride;
			//Console.WriteLine("##### new TLProperty info propertyInfo  " +  propertyInfo + " flags " + IsFlag);
        }

        /// <summary>
        ///     Order in constructor.
        /// </summary>
        public uint Order { get; private set; }

		/// <summary>
		///     Flag order if its a flag
		/// </summary>
		public uint Flag { get; set; }

		/// <summary>
		///     Indicates if it is a flag
		/// </summary>
		public bool IsFlag { get; set; }

        /// <summary>
        ///     Property info.
        /// </summary>
        public PropertyInfo PropertyInfo { get; private set; }

        /// <summary>
        ///     Serialization mode override.
        /// </summary>
        public TLSerializationMode? SerializationModeOverride { get; private set; }
    }
}
