// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLSerializersBucket.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SharpTL.Serializers;

namespace SharpTL
{
    /// <summary>
    ///     TL serialization bucket.
    /// </summary>
    public class TLSerializersBucket
    {
        private static readonly Type _GenericListType = typeof (List<>);
        private static readonly Type _ObjectType = typeof (Object);
        private static readonly Type _GenericTLVectorSerializerType = typeof (TLVectorSerializer<>);
        private readonly Dictionary<uint, ITLSerializer> _constructorNumberSerializersIndex = new Dictionary<uint, ITLSerializer>();
        private readonly Dictionary<Type, ITLSerializer> _serializersIndex = new Dictionary<Type, ITLSerializer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TLSerializersBucket"/> class.
        /// </summary>
        public TLSerializersBucket() : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TLSerializersBucket"/> class.
        /// </summary>
        /// <param name="isDurovMode">In Durov mode Bytes is an alias for String type hence both serializers have the same constructor numbers.</param>
        public TLSerializersBucket(bool isDurovMode)
        {
            // Add base type serializers.
            foreach (var serializer in isDurovMode ? BuiltIn.DurovBaseTypeSerializers : BuiltIn.BaseTypeSerializers)
            {
                Add(serializer);
            }
        }
        
        /// <summary>
        ///     Get TL serializer for an object type.
        /// </summary>
        /// <param name="type">Type of the object.</param>
        /// <returns>TL serializer.</returns>
        public ITLSerializer this[Type type]
        {
            get
            {
                ITLSerializer serializer;
                if (_serializersIndex.TryGetValue(type, out serializer))
                {
                    return serializer;
                }
                PrepareSerializer(type);
                return _serializersIndex.TryGetValue(type, out serializer) ? serializer : null;
            }
        }

        /// <summary>
        ///     Get TL serializer for a constructor number.
        /// </summary>
        /// <param name="constructorNumber">Constructor number.</param>
        /// <returns>TL serializer.</returns>
        public ITLSerializer this[uint constructorNumber]
        {
            get
            {
                ITLSerializer serializer;
                return _constructorNumberSerializersIndex.TryGetValue(constructorNumber, out serializer) ? serializer : null;
            }
        }

        /// <summary>
        ///     Does the bucket contain serializer for a type.
        /// </summary>
        /// <param name="type">Type of an object.</param>
        public bool Contains(Type type)
        {
            return _serializersIndex.ContainsKey(type);
        }

        /// <summary>
        ///     Adds serializer.
        /// </summary>
        /// <param name="serializer">TL serializer.</param>
        public void Add(ITLSerializer serializer)
        {
            Type type = serializer.SupportedType;
            if (!_serializersIndex.ContainsKey(type))
            {
                _serializersIndex.Add(type, serializer);

                var singleConstructorSerializer = serializer as ITLSingleConstructorSerializer;
                if (singleConstructorSerializer != null)
                {
                    IndexType(singleConstructorSerializer.ConstructorNumber, serializer);
                }
                var multipleConstructorSerializer = serializer as ITLMultiConstructorSerializer;
                if (multipleConstructorSerializer != null)
                {
                    foreach (uint constructorNumber in multipleConstructorSerializer.ConstructorNumbers)
                    {
                        IndexType(constructorNumber, serializer);
                    }
                }
            }
        }

        /// <summary>
        ///     Prepare serializer for an object type.
        /// </summary>
        /// <typeparam name="T">Type of an object.</typeparam>
        public void PrepareSerializer<T>()
        {
            PrepareSerializer(typeof (T));
        }

        /// <summary>
        ///     Prepare serializer for an object type.
        /// </summary>
        /// <param name="objType">Object type.</param>
        public void PrepareSerializer(Type objType)
        {
            if (objType == _ObjectType)
            {
                return;
            }

            if (Contains(objType))
            {
                return;
            }

            TypeInfo objTypeInfo = objType.GetTypeInfo();

            // TLType.
            if (objTypeInfo.IsInterface)
            {
                var tlTypeAttribute = objTypeInfo.GetCustomAttribute<TLTypeAttribute>();
                if (tlTypeAttribute == null)
                {
                    return;
                }

                ITLSingleConstructorSerializer[] serializers =
                    tlTypeAttribute.ConstructorTypes.Where(type => objTypeInfo.IsAssignableFrom(type.GetTypeInfo()))
                        .Select(type => this[type] as ITLSingleConstructorSerializer)
                        .Where(s => s != null)
                        .ToArray();
                Add(new TLMultiConstructorObjectSerializer(objType, serializers));

                return;
            }

            if (objTypeInfo.IsAbstract)
            {
                return;
            }

            // TLObject.
            var tlObjectAttribute = objTypeInfo.GetCustomAttribute<TLObjectAttribute>();
            var customSerializerAttribute = objTypeInfo.GetCustomAttribute<TLObjectWithCustomSerializerAttribute>();
            if (tlObjectAttribute != null || customSerializerAttribute != null)
            {
                // Check for custom serializer.
                if (customSerializerAttribute != null)
                {
                    var customSerializer = (ITLSingleConstructorSerializer) Activator.CreateInstance(customSerializerAttribute.Type);
                    if (tlObjectAttribute != null)
                    {
                        customSerializer.ConstructorNumber = tlObjectAttribute.ConstructorNumber;
                    }
                    Add(customSerializer);
                }
                else
                {
                    /*
                     * There is a TLObjectAttribute without custom serializer,
                     * then use this meta-info to create properties map for object serialization based on TLProperty attributes. 
                     */
                    List<TLPropertyInfo> props =
                        objTypeInfo.DeclaredProperties.Zip(
                            objTypeInfo.DeclaredProperties.Select(info => info.GetCustomAttribute<TLPropertyAttribute>()),
                            (info, attribute) => new Tuple<PropertyInfo, TLPropertyAttribute>(info, attribute))
                            .Where(tuple => tuple.Item2 != null)
                            .Select(tuple => new TLPropertyInfo(tuple.Item2.Order, tuple.Item1, tuple.Item2.SerializationModeOverride))
                            .ToList();

                    Add(new TLCustomObjectSerializer(tlObjectAttribute.ConstructorNumber, objType, props, this));
                }
            }
            else
            {
                // Otherwise check for base supported types.
                // List<> will be serialized as built-in type 'vector'.
                if (objTypeInfo.IsGenericType && objType.GetGenericTypeDefinition() == _GenericListType)
                {
                    Type genericVectorSerializerType = _GenericTLVectorSerializerType.MakeGenericType(objTypeInfo.GenericTypeArguments[0]);
                    var serializer = (ITLSerializer) Activator.CreateInstance(genericVectorSerializerType);
                    Add(serializer);
                }
                else
                {
                    throw new NotSupportedException(string.Format("'{0}' is not supported. Only base types and objects with TLObject attribute are supported.", objType));
                }
            }
        }

        private void IndexType(uint constructorNumber, ITLSerializer serializer)
        {
            if (!_constructorNumberSerializersIndex.ContainsKey(constructorNumber))
            {
                _constructorNumberSerializersIndex.Add(constructorNumber, serializer);
            }
        }
    }
}
