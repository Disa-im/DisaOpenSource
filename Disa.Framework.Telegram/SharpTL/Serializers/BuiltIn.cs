// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuiltIn.cs">
//   Copyright (c) 2013-2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace SharpTL.Serializers
{
    /// <summary>
    ///     Built-in stuff.
    /// </summary>
    public static class BuiltIn
    {
        private static List<ITLSerializer> _baseTypeSerializersInternal;

        /// <summary>
        ///     Built-in base type serializers.
        /// </summary>
        public static IEnumerable<ITLSerializer> BaseTypeSerializers
        {
            get
            {
                return _baseTypeSerializersInternal ?? (_baseTypeSerializersInternal = CreateSerializers(false));
            }
        }

        /// <summary>
        ///     Built-in base type serializers in Durov mode.
        /// </summary>
        public static IEnumerable<ITLSerializer> DurovBaseTypeSerializers
        {
            get
            {
                return _baseTypeSerializersInternal ?? (_baseTypeSerializersInternal = CreateSerializers(true));
            }
        }

        private static List<ITLSerializer> CreateSerializers(bool isDurovMode)
        {
            return new List<ITLSerializer>
            {
                new IntSerializer(),
                new UIntSerializer(),
                new LongSerializer(),
                new ULongSerializer(),
                new DoubleSerializer(),
                new StringSerializer(),
                new BooleanSerializer(),
                new TLVectorSerializer<object>(),
                new TLBytesSerializer(isDurovMode),
                new Int128Serializer(),
                new Int256Serializer()
            };
        }
    }
}
