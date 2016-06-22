// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLType.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpTL.Compiler.Utils;

namespace SharpTL.Compiler
{
    /// <summary>
    ///     TL type.
    /// </summary>
    [DebuggerDisplay("{Text}")]
    public class TLType : IEquatable<TLType>
    {
        private static readonly List<string> SystemObjectTypeNames = new List<string> { typeof(Object).FullName, typeof(Object).Name, "object" };
        private int _lastHashCode;
        private string _text;

        public TLType(string name, bool autoConvertToConventionalCase = true)
        {

			//Flag types are have the type #, so we rename to int and let it execute normally

			OriginalName = name;
			Name = autoConvertToConventionalCase ? ConvertToConventionalName(name) : name;
            Constructors = new List<TLCombinator>();
        }

        public string OriginalName { get; private set; }

        /// <summary>
        ///     Name of a TL type.
        /// </summary>
        public string Name { get; set; }

        public bool IsVoid
        {
            get { return Name == "void"; }
        }


        public bool IsSystemObject
        {
            get { return SystemObjectTypeNames.Contains(Name); }
        }

        public uint? Number
        {
            get { return Constructors != null && Constructors.Count > 0 ? Constructors.Select(ctr => ctr.Number).Aggregate((u, u1) => unchecked(u + u1)) : (uint?) null; }
        }

        public bool HasConstructors
        {
            get { return Constructors.Count > 0; }
        }

        public bool IsBuiltIn { get; set; }

        public List<TLCombinator> Constructors { get; set; }

        public TLSerializationMode? SerializationModeOverride { get; set; }

        public string Text
        {
            get { return ToString(); }
        }

        private string ConvertToConventionalName(string name)
        {
            return "I" + name.ToConventionalCase(Case.PascalCase);
        }

        public override string ToString()
        {
            int currentHashCode = GetHashCode();
            if (_lastHashCode != currentHashCode)
            {
                _lastHashCode = currentHashCode;
                _text = string.Format(
                    "{0} 0x{1:X8} (0x{2})",
                    Name,
                    Number,
                    (Constructors != null && Constructors.Count > 0)
                        ? Constructors.Select(u => u.Number.ToString("X8")).Aggregate((paramsText, paramText) => paramsText + " + 0x" + paramText)
                        : string.Empty);
            }
            return _text;
        }

        #region Equality
        public bool Equals(TLType other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return string.Equals(OriginalName, other.OriginalName) && string.Equals(Name, other.Name) && Constructors.SequenceEqual(other.Constructors);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((TLType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (OriginalName != null ? OriginalName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Constructors != null ? Constructors.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(TLType left, TLType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TLType left, TLType right)
        {
            return !Equals(left, right);
        }
        #endregion
    }
}
