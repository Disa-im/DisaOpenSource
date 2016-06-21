// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LongSerializer.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpTL.Serializers
{
    public class LongSerializer : TLBareTypeSerializerBase
    {
        public const uint DefaultConstructorNumber = 0x22076CBAu;
        private static readonly Type _SupportedType = typeof (long);

        public LongSerializer() : base(DefaultConstructorNumber)
        {
        }

        public override Type SupportedType
        {
            get { return _SupportedType; }
        }

        protected override object ReadBody(TLSerializationContext context)
        {
            return context.Streamer.ReadInt64();
        }

        protected override void WriteBody(object obj, TLSerializationContext context)
        {
            context.Streamer.WriteInt64((long) obj);
        }
    }
}
