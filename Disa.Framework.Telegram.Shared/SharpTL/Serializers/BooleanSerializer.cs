// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BooleanSerializer.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SharpTL.Serializers
{
    public class BooleanSerializer : ITLMultiConstructorSerializer
    {
        public const uint FalseConstructorNumber = 0xbc799737;
        public const uint TrueConstructorNumber = 0x997275b5;

        private static readonly uint[] _ConstructorNumbers = {FalseConstructorNumber, TrueConstructorNumber};

        private static readonly Type _SupportedType = typeof (bool);

        public Type SupportedType
        {
            get { return _SupportedType; }
        }

        public IEnumerable<uint> ConstructorNumbers
        {
            get { return _ConstructorNumbers; }
        }

        public void Write(object obj, TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            var value = (bool) obj;
            context.Streamer.WriteUInt32(value ? TrueConstructorNumber : FalseConstructorNumber);
        }

        public object Read(TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            if (modeOverride.HasValue && modeOverride.Value == TLSerializationMode.Bare)
            {
                throw new InvalidOperationException("BooleanSerializer doesn't support bare type serialization.");
            }

            uint constructorNumber = context.Streamer.ReadUInt32();
            if (constructorNumber == TrueConstructorNumber)
            {
                return true;
            }
            if (constructorNumber == FalseConstructorNumber)
            {
                return false;
            }

            throw new InvalidOperationException(string.Format("Invalid boolean value: '{0}', or not supported by current BooleanSerializer.", constructorNumber));
        }
    }
}
