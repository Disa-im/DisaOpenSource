// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLPropertyInfo.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

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
        public TLPropertyInfo(uint order, PropertyInfo propertyInfo, TLSerializationMode? serializationModeOverride = null)
        {
            Order = order;
            PropertyInfo = propertyInfo;
            SerializationModeOverride = serializationModeOverride;
        }

        /// <summary>
        ///     Order in constructor.
        /// </summary>
        public uint Order { get; private set; }

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
