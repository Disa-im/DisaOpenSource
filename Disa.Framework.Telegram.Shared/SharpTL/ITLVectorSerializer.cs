// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITLVectorSerializer.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpTL
{
    /// <summary>
    ///     Interface for TL vector serializer.
    /// </summary>
    public interface ITLVectorSerializer : ITLSerializer
    {
        /// <summary>
        ///     Type of vector's items.
        /// </summary>
        Type ItemsType { get; }

        /// <summary>
        ///     Writes vector to the serialization context.
        /// </summary>
        /// <param name="vector">Vector.</param>
        /// <param name="context">Serialization context.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <param name="itemsModeOverride">Items serialization mode override.</param>
        void Write(object vector, TLSerializationContext context, TLSerializationMode? modeOverride, TLSerializationMode? itemsModeOverride);

        /// <summary>
        ///     Writes vector to the serialization context.
        /// </summary>
        /// <param name="context">Serialization context.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <param name="itemsModeOverride">Items serialization mode override.</param>
        /// <returns>Verctor.</returns>
        object Read(TLSerializationContext context, TLSerializationMode? modeOverride, TLSerializationMode? itemsModeOverride);
    }
}
