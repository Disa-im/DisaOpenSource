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
			//Console.WriteLine("#### New Custom Object Serializer");
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
			bool hasFlags = false;
			uint flagsValue = 0;
            object obj = Activator.CreateInstance(_objectType);
			var properties = obj.GetType().GetProperties();

			foreach (var property in properties) {
				if (property.Name == "Flags") {
					hasFlags = true;
				}
			}
				
			Console.WriteLine("###### has flags" + hasFlags);
			Console.WriteLine("###### The objects type is " + obj.GetType());

			if (hasFlags) {
				ITLPropertySerializationAgent flagAgent = _serializationAgents [0];
				Console.WriteLine("#### just checking the flags serialization agent should be int " + _serializationAgents [0].GetType());
				flagAgent.Read(obj, context);
				flagsValue = (uint)properties[0].GetValue(obj);
				string binary = Convert.ToString(flagsValue,2);
				Console.WriteLine("###### The flags value set is " + flagsValue);
				Console.WriteLine("###### The binary is " + binary);
				for (int i = 1; i < _serializationAgents.Length; i++) {
					var currentPropertyInfo = properties [i];
					var propInfo = currentPropertyInfo.GetCustomAttribute<TLPropertyAttribute>();
					Console.WriteLine("######## The flag index set is " + propInfo.Flag + " and the flag set is" + propInfo.IsFlag);
					//convert the flags to binary
					if (propInfo.IsFlag) {
						if ((binary.Length - 1) < i) {
							Console.WriteLine("###### the length of the binary is smaller exiting");
							continue;
						} else {
							if (binary [i] == '0') {
								Console.WriteLine("The flag is set to zero, exiting");
								continue;
							}
						}
					}
					ITLPropertySerializationAgent agent = _serializationAgents [i];
					agent.Read(obj, context);
				}
			} else {
				for (int i = 0; i < _serializationAgents.Length; i++) {
					ITLPropertySerializationAgent agent = _serializationAgents [i];
					agent.Read(obj, context);
				}
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

		static bool CheckIfHasFlags(IEnumerable<TLPropertyInfo> tlPropertyInfos)
		{
			foreach (var tlPropertyInfo in tlPropertyInfos) {
				if (tlPropertyInfo.IsFlag) {
					return true;
				}
			}
			return false;
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

            private readonly string _propertyName;

            protected TLPropertySerializationAgentBase(TLPropertyInfo tlPropertyInfo)
            {
                TLPropertyInfo = tlPropertyInfo;

                _propertyName = TLPropertyInfo.PropertyInfo.Name;
            }

            public void Write(object obj, TLSerializationContext context)
            {
                object propertyValue = obj.GetType().GetProperty(_propertyName).GetValue(obj);
                WriteValue(propertyValue, context);
            }

            public void Read(object obj, TLSerializationContext context)
            {
                object value = ReadValue(context);
                obj.GetType().GetProperty(_propertyName).SetValue(obj, value);
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
