// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLObjectWithCustomSerializer.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using SharpTL.Annotations;

namespace SharpTL
{
    /// <summary>
    ///     TL object with custom serializer attribute. Used only with <see cref="TLObjectAttribute" />
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true), MeansImplicitUse]
    public class TLObjectWithCustomSerializerAttribute : Attribute
    {
        private static readonly TypeInfo _TLSerializerTypeInfo = typeof (ITLSingleConstructorSerializer).GetTypeInfo();
        private readonly Type _type;

        public TLObjectWithCustomSerializerAttribute(Type serializerType)
        {
            TypeInfo serTypeInfo = serializerType.GetTypeInfo();
            if (!_TLSerializerTypeInfo.IsAssignableFrom(serTypeInfo) || serTypeInfo.IsAbstract || serTypeInfo.IsGenericType)
            {
                throw new TLSerializationException(String.Format("Invalid custom serializer type {0}.", serializerType));
            }

            _type = serializerType;
        }

        /// <summary>
        ///     Custom serializer type must be a non abstract class which implements <see cref="ITLSingleConstructorSerializer" />.
        /// </summary>
        public Type Type
        {
            get { return _type; }
        }
    }
}
