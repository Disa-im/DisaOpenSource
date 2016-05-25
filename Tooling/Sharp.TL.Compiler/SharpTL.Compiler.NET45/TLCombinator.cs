// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLCombinator.cs">
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
    ///     TL combinator.
    /// </summary>
    [DebuggerDisplay("{Text}")]
    public class TLCombinator : IEquatable<TLCombinator>
    {
        private int _lastHashCode;
        private string _text;

        public TLCombinator(string originalName)
        {
            OriginalName = originalName;
            Name = originalName.ToConventionalCase(Case.PascalCase);
        }

        public string OriginalName { get; set; }

        public string Name { get; set; }

        public uint Number { get; set; }

        public List<TLCombinatorParameter> Parameters { get; set; }

        public TLType Type { get; set; }

        public string Text
        {
            get { return ToString(); }
        }

        public override string ToString()
        {
            int currentHashCode = GetHashCode();
            if (_lastHashCode != currentHashCode)
            {
                _lastHashCode = currentHashCode;
                _text = string.Format("{0}#{1:X8} {2} = {3}", Name, Number,
                    (Parameters != null && Parameters.Count > 0)
                        ? Parameters.Select(parameter => parameter.ToString()).Aggregate((paramsText, paramText) => paramsText + " " + paramText)
                        : string.Empty, Type.Name);
            }
            return _text;
        }

        #region Equality
        public bool Equals(TLCombinator other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return string.Equals(Name, other.Name) && Number == other.Number && Equals(Parameters, other.Parameters) && string.Equals(Type, other.Type);
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
            return Equals((TLCombinator) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (int) Number;
                hashCode = (hashCode*397) ^ (OriginalName != null ? OriginalName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Parameters != null ? Parameters.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Type != null ? Type.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(TLCombinator left, TLCombinator right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TLCombinator left, TLCombinator right)
        {
            return !Equals(left, right);
        }
        #endregion
    }
}
