// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLSerializerBase.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using SharpTL.Annotations;

namespace SharpTL.Serializers
{
    /// <summary>
    ///     Base serializer for TL types.
    /// </summary>
    public abstract class TLSerializerBase : ITLSingleConstructorSerializer
    {
        protected TLSerializerBase(uint constructorNumber)
        {
            ConstructorNumber = constructorNumber;
        }

        /// <summary>
        ///     Serialization mode.
        /// </summary>
        public TLSerializationMode SerializationMode { get; protected set; }

        public uint ConstructorNumber { get; set; }

        public abstract Type SupportedType { get; }

        /// <summary>
        ///     Base serializer writes only header with type id. Then calls <see cref="WriteBody" />.
        /// </summary>
        /// <param name="obj">Object to be serialized.</param>
        /// <param name="context">Serialization context.</param>
        /// <param name="modeOverride">Override of default type serialization mode.</param>
        public virtual void Write([NotNull] object obj, TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (obj.GetType() != SupportedType)
            {
                throw new TLSerializationException(string.Format("Expected object of type {0}, actual object type {1}.", SupportedType, obj.GetType()));
            }

            WriteHeader(context, modeOverride);
            WriteBody(obj, context);
        }

        public virtual object Read(TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            ReadAndCheckConstructorNumber(context, modeOverride);
            return ReadBody(context);
        }

        /// <summary>
        ///     Reads and checks constructor number.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="modeOverride">Mode override.</param>
        /// <exception cref="InvalidTLConstructorNumberException">When actual constructor number is not as expected.</exception>
        protected void ReadAndCheckConstructorNumber(TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            if ((!modeOverride.HasValue && SerializationMode != TLSerializationMode.Bare) || (modeOverride.HasValue && modeOverride.Value == TLSerializationMode.Boxed))
            {
                // If type is boxed (not bare) then read type constructor number and check for supporting.
                uint constructorNumber = context.Streamer.ReadUInt32();
                if (constructorNumber != ConstructorNumber)
                {
                    throw new InvalidTLConstructorNumberException(string.Format("Invalid TL constructor number. Expected: {0}, actual: {1}.", ConstructorNumber,
                        constructorNumber));
                }
            }
        }

        /// <summary>
        ///     Writes a header with constructor number.
        /// </summary>
        /// <param name="context">Serialization context.</param>
        /// <param name="modeOverride">Serialization mode override.</param>
        protected virtual void WriteHeader(TLSerializationContext context, TLSerializationMode? modeOverride = null)
        {
            if ((!modeOverride.HasValue && SerializationMode != TLSerializationMode.Bare) || (modeOverride.HasValue && modeOverride.Value == TLSerializationMode.Boxed))
            {
                // If type is boxed (not bare) then write type constructor number.
                context.Streamer.WriteUInt32(ConstructorNumber);
            }
        }

        /// <summary>
        ///     Reads a body.
        /// </summary>
        /// <param name="context">Serialization context.</param>
        /// <returns>Body of an object.</returns>
        protected abstract object ReadBody(TLSerializationContext context);

        /// <summary>
        ///     Write a body.
        /// </summary>
        /// <param name="obj">Object to be serialized.</param>
        /// <param name="context">Serialization context.</param>
        protected abstract void WriteBody(object obj, TLSerializationContext context);
    }

    /// <summary>
    ///     Base serializer for TL bare types.
    /// </summary>
    public abstract class TLBareTypeSerializerBase : TLSerializerBase
    {
        protected TLBareTypeSerializerBase(uint constructorNumber) : base(constructorNumber)
        {
            SerializationMode = TLSerializationMode.Bare;
        }
    }

    /// <summary>
    ///     Base serializer for TL boxed types.
    /// </summary>
    public abstract class TLBoxedTypeSerializerBase : TLSerializerBase
    {
        protected TLBoxedTypeSerializerBase(uint constructorNumber) : base(constructorNumber)
        {
            SerializationMode = TLSerializationMode.Boxed;
        }
    }
}
