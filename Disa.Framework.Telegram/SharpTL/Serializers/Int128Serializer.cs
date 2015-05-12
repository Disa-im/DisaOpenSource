// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Int128Serializer.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using BigMath;

namespace SharpTL.Serializers
{
    /// <summary>
    ///     Serializer for 128-bit integer.
    /// </summary>
    public class Int128Serializer : TLBareTypeSerializerBase
    {
        public const uint DefaultConstructorNumber = 0x84CCF7B7;
        private static readonly Type _SupportedType = typeof (Int128);

        public Int128Serializer() : base(DefaultConstructorNumber)
        {
        }

        public override Type SupportedType
        {
            get { return _SupportedType; }
        }

        protected override object ReadBody(TLSerializationContext context)
        {
            return context.Streamer.ReadInt128();
        }

        protected override void WriteBody(object obj, TLSerializationContext context)
        {
            context.Streamer.WriteInt128((Int128) obj);
        }
    }
}
