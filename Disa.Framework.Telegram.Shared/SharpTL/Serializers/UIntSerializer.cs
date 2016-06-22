// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UIntSerializer.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpTL.Serializers
{
    public class UIntSerializer : TLBareTypeSerializerBase
    {
        public const uint DefaultConstructorNumber = 0xA8509BDAu;
        private static readonly Type _SupportedType = typeof (uint);

        public UIntSerializer() : base(DefaultConstructorNumber)
        {
        }

        public override Type SupportedType
        {
            get { return _SupportedType; }
        }

        protected override object ReadBody(TLSerializationContext context)
        {
            return context.Streamer.ReadUInt32();
        }

        protected override void WriteBody(object obj, TLSerializationContext context)
        {
            context.Streamer.WriteUInt32((uint) obj);
        }
    }
}
