// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLTypeAttribute.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;

namespace SharpTL
{
    /// <summary>
    ///     TL type attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TLTypeAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TLTypeAttribute" /> class.
        /// </summary>
        /// <param name="constructorTypes">Constructor types.</param>
        public TLTypeAttribute(params Type[] constructorTypes)
        {
            var constructorTypesList = new List<Type>();
            uint typeNumber = 0;

            foreach (Type constructorType in constructorTypes)
            {
                var tlObjectAttribute = constructorType.GetTypeInfo().GetCustomAttribute<TLObjectAttribute>();
                if (tlObjectAttribute == null)
                {
                    // TODO: log that constructor type has no TLObject attribute.
                    continue;
                }
                constructorTypesList.Add(constructorType);
                typeNumber = unchecked(typeNumber + tlObjectAttribute.ConstructorNumber);
            }

            ConstructorTypes = constructorTypesList;
            TypeNumber = typeNumber;
        }

        /// <summary>
        ///     Constructor types.
        /// </summary>
        public IEnumerable<Type> ConstructorTypes { get; private set; }

        /// <summary>
        ///     Type number.
        /// </summary>
        public uint TypeNumber { get; private set; }
    }
}
