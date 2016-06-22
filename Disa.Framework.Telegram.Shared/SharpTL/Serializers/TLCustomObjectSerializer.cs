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
			bool hasFlags = false;
			uint flagsValue = 0;

			hasFlags = CheckForFlags(obj);

			if (hasFlags)
			{
				var properties = obj.GetType().GetProperties();
				flagsValue = (uint)properties[0].GetValue(obj);
				string binary = Convert.ToString(flagsValue, 2);

				var maxIndex = binary.Length - 1;
				for (int i = 0; i < _serializationAgents.Length; i++)
				{
					var currentPropertyInfo = properties[i];
					var propInfo = currentPropertyInfo.GetCustomAttribute<TLPropertyAttribute>();
					//convert the flags to binary
					if (propInfo.IsFlag)
					{
						if (maxIndex < propInfo.Flag)
						{//since the length is smaller than the flag index, it means this flag was not sent in the msg
							continue;
						} 
						if (binary[(int)(maxIndex - propInfo.Flag)] == '0')
						{//if the flag is set to zero
							continue;
						}
					}
					ITLPropertySerializationAgent agent = _serializationAgents[i];
					agent.Write(obj, context);
				}
			}
			else
			{
				for (int i = 0; i < _serializationAgents.Length; i++)
				{
					ITLPropertySerializationAgent agent = _serializationAgents[i];
					agent.Write(obj, context);
				}
			}
		}

		protected override object ReadBody(TLSerializationContext context)
		{
			bool hasFlags = false;
			uint flagsValue = 0;

			object obj = Activator.CreateInstance(_objectType);

			hasFlags = CheckForFlags(obj);

			if (hasFlags)
			{
				var properties = obj.GetType().GetProperties();
				ITLPropertySerializationAgent flagAgent = _serializationAgents[0];
				//deserialize anf read the flags
				flagAgent.Read(obj, context);
				flagsValue = (uint)properties[0].GetValue(obj);
				//convert it to binary
				string binary = Convert.ToString(flagsValue, 2);
				//the length of the binary is the maximum index of flasg in the message,
				//anything greater than this index, is useless and should directly exit
				var maxIndex = binary.Length - 1;
				for (int i = 1; i < _serializationAgents.Length; i++)
				{
					var currentPropertyInfo = properties[i];
					var propInfo = currentPropertyInfo.GetCustomAttribute<TLPropertyAttribute>();
					//convert the flags to binary
					if (propInfo.IsFlag)
					{
						if (maxIndex < propInfo.Flag)
						{//since the length is smaller than the flag index, it means this flag was not sent in the msg
							continue;
						} 
						if (binary[(int)(maxIndex - propInfo.Flag)] == '0')
						{//if the flag is set to zero
							continue;
						}
					}
					ITLPropertySerializationAgent agent = _serializationAgents[i];
					agent.Read(obj, context);
				}
			}
			else
			{
				for (int i = 0; i < _serializationAgents.Length; i++)
				{
					ITLPropertySerializationAgent agent = _serializationAgents[i];
					agent.Read(obj, context);
				}
			}
			return obj;
		}

		bool CheckForFlags(object obj)
		{
			var properties = obj.GetType().GetProperties();

			foreach (var property in properties)
			{
				if (property.Name == "Flags")
				{
					return true;
				}
			}
			return false;
		}

		private static ITLPropertySerializationAgent[] CreateSerializationAgents(IEnumerable<TLPropertyInfo> tlPropertyInfos, TLSerializersBucket serializersBucket)
		{
			return tlPropertyInfos.OrderBy(info => info.Order).Distinct().Select(
				tlPropertyInfo =>
				{
					PropertyInfo propertyInfo = tlPropertyInfo.PropertyInfo;

					Type propType = propertyInfo.PropertyType;

					ITLPropertySerializationAgent serializationAgent;

					if (propType == typeof(object))
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
			public TLObjectPropertySerializationAgent(TLPropertyInfo tlPropertyInfo)
				: base(tlPropertyInfo)
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

			public TLPropertySerializationAgent(TLPropertyInfo tlPropertyInfo, ITLSerializer serializer)
				: base(tlPropertyInfo)
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
