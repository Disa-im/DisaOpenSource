// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLCustomObjectSerializer.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Dynamitey;

namespace SharpTL.Serializers
{
    /// <summary>
    ///     Serializer for TL custom object.
    /// </summary>
    public class TLCustomObjectSerializer : TLSerializerBase
    {
        private readonly Type _objectType;

        private readonly ITLPropertySerializationAgent[] _serializationAgents;

        public TLCustomObjectSerializer(
            uint constructorNumber,
            Type objectType,
            IEnumerable<TLPropertyInfo> properties,
            TLSerializersBucket serializersBucket,
            TLSerializationMode serializationMode = TLSerializationMode.Boxed)
            : base(constructorNumber)
        {
            _objectType = objectType;
            _serializationAgents = CreateSerializationAgents(properties, serializersBucket);
            SerializationMode = serializationMode;
        }

        public override Type SupportedType
        {
            get { return _objectType; }
        }

        protected override void WriteBody(object obj, TLSerializationContext context)
        {
            for (int i = 0; i < _serializationAgents.Length; i++)
            {
                ITLPropertySerializationAgent agent = _serializationAgents[i];
                agent.Write(obj, context);
            }
        }

        protected override object ReadBody(TLSerializationContext context)
        {
            object obj = Activator.CreateInstance(_objectType);
            for (int i = 0; i < _serializationAgents.Length; i++)
            {
                ITLPropertySerializationAgent agent = _serializationAgents[i];
                agent.Read(obj, context);
            }
            return obj;
        }

        private static ITLPropertySerializationAgent[] CreateSerializationAgents(IEnumerable<TLPropertyInfo> tlPropertyInfos, TLSerializersBucket serializersBucket)
        {
            return tlPropertyInfos.OrderBy(info => info.Order).Distinct().Select(
                tlPropertyInfo =>
                {
                    PropertyInfo propertyInfo = tlPropertyInfo.PropertyInfo;

                    Type propType = propertyInfo.PropertyType;

                    ITLPropertySerializationAgent serializationAgent;

                    if (propType == typeof (object))
                    {
                        /*
                         * https://core.telegram.org/mtproto/serialize#object-pseudotype
                         * Object Pseudotype
                         * The Object pseudotype is a “type” which can take on values that belong to any boxed type in the schema.
                         */
                        serializationAgent = new TLObjectPropertySerializationAgent(tlPropertyInfo);
                    }
                    else
                    {
                        ITLSerializer tlSerializer = serializersBucket[propType];
                        Debug.Assert(tlSerializer != null);

                        var vectorSerializer = tlSerializer as ITLVectorSerializer;
                        if (vectorSerializer != null)
                        {
                            TLSerializationMode? itemsSerializationModeOverride = GetVectorItemsSerializationModeOverride(
                                vectorSerializer,
                                propertyInfo,
                                serializersBucket);
                            serializationAgent = new TLVectorPropertySerializationAgent(tlPropertyInfo, vectorSerializer, itemsSerializationModeOverride);
                        }
                        else
                        {
                            serializationAgent = new TLPropertySerializationAgent(tlPropertyInfo, tlSerializer);
                        }
                    }
                    return serializationAgent;
                }).ToArray();
        }

        private static TLSerializationMode? GetVectorItemsSerializationModeOverride(
            ITLVectorSerializer vectorSerializer,
            PropertyInfo propertyInfo,
            TLSerializersBucket serializersBucket)
        {
            Type propType = propertyInfo.PropertyType;

            if (vectorSerializer.SupportedType != propType)
            {
                throw new NotSupportedException(
                    string.Format("Current vector serializer doesn't support type: {0}. It supports: {1}", propType, vectorSerializer.SupportedType));
            }

            TLSerializationMode? itemsSerializationModeOverride = TLSerializationMode.Bare;

            // Check for items serializer.
            // If items have multiple constructors or have a TLTypeAttribute (in other words it is TL type),
            // then items must be serialized as boxed.
            Type itemsType = vectorSerializer.ItemsType;
            ITLSerializer vectorItemSerializer = serializersBucket[itemsType];
            if (vectorItemSerializer is ITLMultiConstructorSerializer || itemsType.GetTypeInfo().GetCustomAttribute<TLTypeAttribute>() != null)
            {
                itemsSerializationModeOverride = TLSerializationMode.Boxed;
            }
            else
            {
                // Check for TLVector attribute with items serialization mode override.
                var tlVectorAttribute = propertyInfo.GetCustomAttribute<TLVectorAttribute>();
                if (tlVectorAttribute != null)
                {
                    itemsSerializationModeOverride = tlVectorAttribute.ItemsModeOverride;
                }
            }
            return itemsSerializationModeOverride;
        }

        #region Property serialization agents.
        /// <summary>
        ///     TL property serialization agent.
        /// </summary>
        private interface ITLPropertySerializationAgent
        {
            void Write(object obj, TLSerializationContext context);
            void Read(object obj, TLSerializationContext context);
        }

        /// <summary>
        ///     Base TL property serialization agent.
        /// </summary>
        private abstract class TLPropertySerializationAgentBase : ITLPropertySerializationAgent
        {
            protected readonly TLPropertyInfo TLPropertyInfo;

            private readonly CacheableInvocation _get;
            private readonly CacheableInvocation _set;

            protected TLPropertySerializationAgentBase(TLPropertyInfo tlPropertyInfo)
            {
                TLPropertyInfo = tlPropertyInfo;

                string propertyName = TLPropertyInfo.PropertyInfo.Name;

                _get = new CacheableInvocation(InvocationKind.Get, propertyName);
                _set = new CacheableInvocation(InvocationKind.Set, propertyName, 1);
            }

            public void Write(object obj, TLSerializationContext context)
            {
                object propertyValue = _get.Invoke(obj);
                WriteValue(propertyValue, context);
            }

            public void Read(object obj, TLSerializationContext context)
            {
                object value = ReadValue(context);
                _set.Invoke(obj, value);
            }

            protected abstract void WriteValue(object propertyValue, TLSerializationContext context);
            protected abstract object ReadValue(TLSerializationContext context);
        }

        /// <summary>
        ///     TLObject property serialization agent.
        /// </summary>
        private class TLObjectPropertySerializationAgent : TLPropertySerializationAgentBase
        {
            public TLObjectPropertySerializationAgent(TLPropertyInfo tlPropertyInfo) : base(tlPropertyInfo)
            {
            }

            protected override void WriteValue(object propertyValue, TLSerializationContext context)
            {
                TLRig.Serialize(propertyValue, context, TLSerializationMode.Boxed);
            }

            protected override object ReadValue(TLSerializationContext context)
            {
                return TLRig.Deserialize<object>(context, TLSerializationMode.Boxed);
            }
        }

        /// <summary>
        ///     Regular TL property serialization agent.
        /// </summary>
        private class TLPropertySerializationAgent : TLPropertySerializationAgentBase
        {
            private readonly ITLSerializer _serializer;

            public TLPropertySerializationAgent(TLPropertyInfo tlPropertyInfo, ITLSerializer serializer) : base(tlPropertyInfo)
            {
                _serializer = serializer;
            }

            protected override void WriteValue(object propertyValue, TLSerializationContext context)
            {
                _serializer.Write(propertyValue, context, TLPropertyInfo.SerializationModeOverride);
            }

            protected override object ReadValue(TLSerializationContext context)
            {
                return _serializer.Read(context, TLPropertyInfo.SerializationModeOverride);
            }
        }

        /// <summary>
        ///     TL vector property serialization agent.
        /// </summary>
        private class TLVectorPropertySerializationAgent : TLPropertySerializationAgentBase
        {
            private readonly ITLVectorSerializer _serializer;
            private readonly TLSerializationMode? _itemsSerializationModeOverride;

            public TLVectorPropertySerializationAgent(TLPropertyInfo tlPropertyInfo, ITLVectorSerializer serializer, TLSerializationMode? itemsSerializationModeOverride)
                : base(tlPropertyInfo)
            {
                _serializer = serializer;
                _itemsSerializationModeOverride = itemsSerializationModeOverride;
            }

            protected override void WriteValue(object propertyValue, TLSerializationContext context)
            {
                _serializer.Write(propertyValue, context, TLPropertyInfo.SerializationModeOverride, _itemsSerializationModeOverride);
            }

            protected override object ReadValue(TLSerializationContext context)
            {
                return _serializer.Read(context, TLPropertyInfo.SerializationModeOverride, _itemsSerializationModeOverride);
            }
        }
        #endregion
    }
}
