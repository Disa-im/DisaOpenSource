// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLVectorSerializer.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SharpTL.Serializers
{
    public class TLVectorSerializer<T> : TLBoxedTypeSerializerBase, ITLVectorSerializer
    {
        public const uint DefaultConstructorNumber = 0x1CB5C415;
        private const TLSerializationMode DefaultItemsSerializationMode = TLSerializationMode.Boxed;
        private static readonly Type ItemsTypeInternal = typeof (T);
        private static readonly Type SupportedTypeInternal = typeof (List<T>);
        // ReSharper disable once StaticFieldInGenericType
        private static readonly bool IsItemsTypeObject;

        static TLVectorSerializer()
        {
            IsItemsTypeObject = ItemsTypeInternal == typeof (Object);
        }

        public TLVectorSerializer()
            : base(DefaultConstructorNumber)
        {
        }

        public override Type SupportedType
        {
            get { return SupportedTypeInternal; }
        }

        public Type ItemsType
        {
            get { return ItemsTypeInternal; }
        }

        public void Write(object vector, TLSerializationContext context, TLSerializationMode? modeOverride, TLSerializationMode? itemsModeOverride)
        {
            WriteHeader(context, modeOverride);
            WriteBodyInternal(vector, context, itemsModeOverride);
        }

        public object Read(TLSerializationContext context, TLSerializationMode? modeOverride, TLSerializationMode? itemsModeOverride)
        {
            ReadAndCheckConstructorNumber(context, modeOverride);
            return ReadBodyInternal(context, itemsModeOverride);
        }

        protected override object ReadBody(TLSerializationContext context)
        {
            return ReadBodyInternal(context, DefaultItemsSerializationMode);
        }

        protected override void WriteBody(object obj, TLSerializationContext context)
        {
            WriteBodyInternal(obj, context, DefaultItemsSerializationMode);
        }

        private object ReadBodyInternal(TLSerializationContext context, TLSerializationMode? itemsSerializationModeOverride = null)
        {
            Func<TLSerializationContext, TLSerializationMode?, object> read;
            if (IsItemsTypeObject)
            {
                read = (sc, m) => TLRig.Deserialize<T>(sc, m);
            }
            else
            {
                ITLSerializer serializer = GetSerializer(context);
                read = serializer.Read;
            }

            int length = context.Streamer.ReadInt32();
            var list = (List<T>) Activator.CreateInstance(SupportedTypeInternal, length);

            for (int i = 0; i < length; i++)
            {
                var item = (T) read(context, itemsSerializationModeOverride);
                list.Add(item);
            }

            return list;
        }

        private void WriteBodyInternal(object obj, TLSerializationContext context, TLSerializationMode? itemsSerializationModeOverride = null)
        {
            Action<object, TLSerializationContext, TLSerializationMode?> write;
            if (IsItemsTypeObject)
            {
                write = TLRig.Serialize;
            }
            else
            {
                ITLSerializer serializer = GetSerializer(context);
                write = serializer.Write;
            }

            var vector = obj as List<T>;
            if (vector == null)
            {
                // TODO: log wrong type.
                throw new InvalidOperationException("This serializer supports only List<> types.");
            }
            int length = vector.Count;

            // Length.
            context.Streamer.WriteInt32(length);

            // Child objects.
            for (int i = 0; i < length; i++)
            {
                write(vector[i], context, itemsSerializationModeOverride);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ITLSerializer GetSerializer(TLSerializationContext context)
        {
            ITLSerializer serializer = context.Rig.GetSerializer<T>();
            if (serializer == null)
            {
                throw new TLSerializerNotFoundException(string.Format("There is no serializer for a type: '{0}'.", ItemsTypeInternal.FullName));
            }
            return serializer;
        }
    }
}
