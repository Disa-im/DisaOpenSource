// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITLSerializer.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SharpTL
{
    /// <summary>
    /// Interface of a TL serializer which supports only single constructor.
    /// </summary>
    public interface ITLSingleConstructorSerializer : ITLSerializer
    {
        /// <summary>
        /// Constructor number.
        /// </summary>
        uint ConstructorNumber { get; set; }
    }

    /// <summary>
    /// Interface of a TL serializer which supports miltiple constructors.
    /// </summary>
    public interface ITLMultiConstructorSerializer : ITLSerializer
    {
        /// <summary>
        /// Constructor numbers.
        /// </summary>
        IEnumerable<uint> ConstructorNumbers { get; }
    }

    /// <summary>
    /// Interface of a TL serializer.
    /// </summary>
    public interface ITLSerializer
    {
        /// <summary>
        /// Supported type.
        /// </summary>
        Type SupportedType { get; }

        /// <summary>
        ///     Serializes an object.
        /// </summary>
        /// <param name="obj">Object to be serialized.</param>
        /// <param name="context">Serialization context.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        void Write(object obj, TLSerializationContext context, TLSerializationMode? modeOverride = null);

        /// <summary>
        /// Deserialize an object.
        /// </summary>
        /// <param name="context">Serialization context.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        /// <returns></returns>
        object Read(TLSerializationContext context, TLSerializationMode? modeOverride = null);
    }
}
