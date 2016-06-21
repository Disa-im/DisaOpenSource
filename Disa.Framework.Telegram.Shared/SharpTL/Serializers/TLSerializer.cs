// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLSerializer.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpTL.Serializers
{
    /// <summary>
    ///     Generic TL serializer.
    /// </summary>
    /// <typeparam name="T">Type of a serialized object.</typeparam>
    public abstract class TLSerializer<T> : TLSerializerBase
    {
        private static readonly Type ObjType = typeof (T);
        
        protected TLSerializer(uint constructorNumber) : base(constructorNumber)
        {
        }

        public override Type SupportedType
        {
            get { return ObjType; }
        }

        protected sealed override object ReadBody(TLSerializationContext context)
        {
            return ReadTypedBody(context);
        }

        protected sealed override void WriteBody(object obj, TLSerializationContext context)
        {
            WriteTypedBody((T) obj, context);
        }

        protected abstract T ReadTypedBody(TLSerializationContext context);
        protected abstract void WriteTypedBody(T obj, TLSerializationContext context);
    }
}
