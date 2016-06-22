// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringSerializer.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace SharpTL.Serializers
{
    public class StringSerializer : TLBareTypeSerializerBase
    {
        public const uint DefaultConstructorNumber = 0xB5286E24;
        private static readonly Type _SupportedType = typeof (string);

        public StringSerializer() : base(DefaultConstructorNumber)
        {
        }

        public override Type SupportedType
        {
            get { return _SupportedType; }
        }

        private static Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }

        protected override object ReadBody(TLSerializationContext context)
        {
            byte[] bytes = context.Streamer.ReadTLBytes();
            return Encoding.GetString(bytes, 0, bytes.Length);
        }

        protected override void WriteBody(object obj, TLSerializationContext context)
        {
            var str = (string) obj;
            TLStreamer streamer = context.Streamer;

            byte[] bytes = Encoding.GetBytes(str);

            streamer.WriteTLBytes(bytes);
        }
    }
}
