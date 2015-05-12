// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLMultiConstructorObjectSerializer.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpTL.Serializers
{
    /// <summary>
    ///     Serializer for TL object with multiple constructors.
    /// </summary>
    public class TLMultiConstructorObjectSerializer : ITLMultiConstructorSerializer
    {
        private readonly Type _objectType;
        private readonly Dictionary<uint, ITLSingleConstructorSerializer> _serializersConstructorNumberIndex;
        private readonly Dictionary<Type, ITLSingleConstructorSerializer> _serializersTypeIndex;

        public TLMultiConstructorObjectSerializer(Type objectType, IEnumerable<ITLSingleConstructorSerializer> serializers)
        {
            _objectType = objectType;
            _serializersTypeIndex = serializers.ToDictionary(serializer => serializer.SupportedType);
            _serializersConstructorNumberIndex = _serializersTypeIndex.Values.ToDictionary(serializer => serializer.ConstructorNumber);
            ConstructorNumbers = new List<uint>(_serializersConstructorNumberIndex.Keys);
        }

        public Type SupportedType
        {
            get { return _objectType; }
        }

        public void Write(object obj, TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            Type objType = obj.GetType();
            ITLSingleConstructorSerializer serializer;
            if (!_serializersTypeIndex.TryGetValue(objType, out serializer))
            {
                throw new NotSupportedException(string.Format("Object type '{0}' is not supported by this serializer.", objType));
            }

            TLRig.Serialize(obj, context, modeOverride);
        }

        public object Read(TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            if (modeOverride.HasValue && modeOverride.Value == TLSerializationMode.Bare)
            {
                throw new InvalidOperationException("TLMultiConstructorObjectSerializer doesn't support bare type deserialization.");
            }

            uint constructorNumber = context.Streamer.ReadUInt32();
            ITLSingleConstructorSerializer serializer;
            if (!_serializersConstructorNumberIndex.TryGetValue(constructorNumber, out serializer))
            {
                throw new NotSupportedException(string.Format("Construction number 0x{0:X} is not supported by this serializer.", constructorNumber));
            }

            return serializer.Read(context, TLSerializationMode.Bare);
        }

        public IEnumerable<uint> ConstructorNumbers { get; private set; }
    }
}
