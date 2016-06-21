// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLObjectAttribute.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using SharpTL.Annotations;

namespace SharpTL
{
    /// <summary>
    ///     TL object attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true), MeansImplicitUse]
    public class TLObjectAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TLObjectAttribute" /> class.
        /// </summary>
        /// <param name="constructorNumber">Constructor number.</param>
        public TLObjectAttribute(uint constructorNumber)
        {
            ConstructorNumber = constructorNumber;
        }

        /// <summary>
        ///     Constructor number.
        /// </summary>
        public uint ConstructorNumber { get; private set; }
    }
}
