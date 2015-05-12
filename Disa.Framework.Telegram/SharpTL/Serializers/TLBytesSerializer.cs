// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLBytesSerializer.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpTL.Serializers
{
    public class TLBytesSerializer : TLBareTypeSerializerBase
    {
        public const uint DefaultDurovConstructorNumber = 0xB5286E24;
        public const uint DefaultConstructorNumber = 0xEBEFB69E;
        private static readonly Type _SupportedType = typeof (byte[]);

        private readonly bool _isDurovMode;

        public TLBytesSerializer() : this(false)
        {
        }

        public TLBytesSerializer(bool isDurovMode) : base(isDurovMode ? DefaultDurovConstructorNumber : DefaultConstructorNumber)
        {
            _isDurovMode = isDurovMode;
        }

        /// <summary>
        ///     In Durov mode Bytes is an alias for String type hence both serializers have the same constructor numbers.
        /// </summary>
        /// <remarks>
        ///     TL bytes contructor number of normal systems: 0xEBEFB69E,
        ///     TL bytes constructor number of Durov's systems: 0xB5286E24 (yes, like string).
        /// </remarks>
        public bool IsDurovMode
        {
            get { return _isDurovMode; }
        }

        public override Type SupportedType
        {
            get { return _SupportedType; }
        }

        protected override object ReadBody(TLSerializationContext context)
        {
            return context.Streamer.ReadTLBytes();
        }

        protected override void WriteBody(object obj, TLSerializationContext context)
        {
            context.Streamer.WriteTLBytes((byte[]) obj);
        }
    }
}
