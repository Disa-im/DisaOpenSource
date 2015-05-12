// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleSerializer.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpTL.Serializers
{
    public class DoubleSerializer : TLBareTypeSerializerBase
    {
        public const uint DefaultConstructorNumber = 0x2210C154u;
        private static readonly Type _SupportedType = typeof (double);

        public DoubleSerializer() : base(DefaultConstructorNumber)
        {
        }

        public override Type SupportedType
        {
            get { return _SupportedType; }
        }

        protected override object ReadBody(TLSerializationContext context)
        {
            return context.Streamer.ReadDouble();
        }

        protected override void WriteBody(object obj, TLSerializationContext context)
        {
            context.Streamer.WriteDouble((double) obj);
        }
    }
}
