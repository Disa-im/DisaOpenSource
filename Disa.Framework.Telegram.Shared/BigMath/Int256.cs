// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Int256.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BigMath.Utils;

namespace BigMath
{
    /// <summary>
    ///     Represents a 256-bit signed integer.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 32)]
    public struct Int256 : IComparable<Int256>, IComparable, IEquatable<Int256>, IFormattable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [FieldOffset(0)] private ulong _d;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [FieldOffset(8)] private ulong _c;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [FieldOffset(16)] private ulong _b;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [FieldOffset(32)] private ulong _a;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get { return "0x" + ToString("X1"); }
        }

        private const ulong NegativeSignMask = 0x1UL << 63;

        /// <summary>
        ///     Gets a value that represents the number 0 (zero).
        /// </summary>
        public static Int256 Zero = GetZero();

        /// <summary>
        ///     Represents the largest possible value of an Int256.
        /// </summary>
        public static Int256 MaxValue = GetMaxValue();

        /// <summary>
        ///     Represents the smallest possible value of an Int256.
        /// </summary>
        public static Int256 MinValue = GetMinValue();

        private static Int256 GetMaxValue()
        {
            return new Int256(long.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
        }

        private static Int256 GetMinValue()
        {
            return -GetMaxValue();
        }

        private static Int256 GetZero()
        {
            return new Int256();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(byte value)
        {
            _a = 0;
            _b = 0;
            _c = 0;
            _d = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public Int256(bool value)
        {
            _a = 0;
            _b = 0;
            _c = 0;
            _d = (ulong) (value ? 1 : 0);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(char value)
        {
            _a = 0;
            _b = 0;
            _c = 0;
            _d = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(decimal value)
        {
            bool isNegative = value < 0;
            uint[] bits = decimal.GetBits(value).ConvertAll(i => (uint) i);
            uint scale = (bits[3] >> 16) & 0x1F;
            if (scale > 0)
            {
                uint[] quotient;
                uint[] reminder;
                MathUtils.DivModUnsigned(bits, new[] { 10U * scale }, out quotient, out reminder);

                bits = quotient;
            }

            _a = 0;
            _b = 0;
            _c = bits[2];
            _d = bits[0] | (ulong) bits[1] << 32;

            if (isNegative)
            {
                Negate();
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(double value) : this((decimal) value)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(float value) : this((decimal) value)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(short value) : this((int) value)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(int value) : this((long) value)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(long value)
        {
            _a = _b = _c = unchecked((ulong) (value < 0 ? ~0 : 0));
            _d = (ulong) value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(sbyte value) : this((long) value)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(ushort value)
        {
            _a = 0;
            _b = 0;
            _c = 0;
            _d = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(uint value)
        {
            _a = 0;
            _b = 0;
            _c = 0;
            _d = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(ulong value)
        {
            _a = 0;
            _b = 0;
            _c = 0;
            _d = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(Guid value)
        {
            var int256 = value.ToByteArray().ToInt256(0);
            _a = int256.A;
            _b = int256.B;
            _c = int256.C;
            _d = int256.D;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int256(Int128 value)
        {
            ulong[] values = value.ToUIn64Array();
            _a = _b = unchecked((ulong) (value.Sign < 0 ? ~0 : 0));
            _c = values[1];
            _d = values[0];
        }

        public Int256(ulong a, ulong b, ulong c, ulong d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int256" /> struct.
        /// </summary>
        /// <param name="sign">The sign.</param>
        /// <param name="ints">The ints.</param>
        public Int256(int sign, uint[] ints)
        {
            if (ints == null)
            {
                throw new ArgumentNullException("ints");
            }

            var value = new ulong[4];
            for (int i = 0; i < ints.Length && i < 8; i++)
            {
                Buffer.BlockCopy(ints[i].ToBytes(), 0, value, i*4, 4);
            }

            _a = value[3];
            _b = value[2];
            _c = value[1];
            _d = value[0];

            if (sign < 0 && (_d > 0 || _c > 0 || _b > 0 || _a > 0))
            {
                // We use here two's complement numbers representation,
                // hence such operations for negative numbers.
                Negate();
                _a |= NegativeSignMask; // Ensure negative sign.
            }
        }

        /// <summary>
        ///     Higher 64 bits of the higher 128 bits.
        /// </summary>
        public ulong A
        {
            get { return _a; }
        }

        /// <summary>
        ///     Lower 64 bits of the higher 128 bits.
        /// </summary>
        public ulong B
        {
            get { return _b; }
        }

        /// <summary>
        ///     Higher 64 bits of the lower 128 bits.
        /// </summary>
        public ulong C
        {
            get { return _c; }
        }

        /// <summary>
        ///     Lower 64 bits of the lower 128 bits.
        /// </summary>
        public ulong D
        {
            get { return _d; }
        }

        /// <summary>
        ///     Gets a number that indicates the sign (negative, positive, or zero) of the current Int256 object.
        /// </summary>
        /// <value>A number that indicates the sign of the Int256 object</value>
        public int Sign
        {
            get
            {
                if (_a == 0 && _b == 0 && _c == 0 && _d == 0)
                {
                    return 0;
                }

                return ((_a & NegativeSignMask) == 0) ? 1 : -1;
            }
        }

        /// <summary>
        ///     Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return _a.GetHashCode() ^ _b.GetHashCode() ^ _c.GetHashCode() ^ _d.GetHashCode();
        }

        /// <summary>
        ///     Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        ///     true if obj has the same value as this instance; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        ///     Returns a value indicating whether this instance is equal to a specified Int64 value.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>
        ///     true if obj has the same value as this instance; otherwise, false.
        /// </returns>
        public bool Equals(Int256 obj)
        {
            return _a == obj._a && _b == obj._b && _c == obj._c && _d == obj._d;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, null);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="format">The format. Only x, X, g, G, d, D are supported.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information about this instance.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider = null)
        {
            if (formatProvider == null)
            {
                formatProvider = CultureInfo.CurrentCulture;
            }
            
            if (!string.IsNullOrEmpty(format))
            {
                char ch = format[0];
                if ((ch == 'x') || (ch == 'X'))
                {
                    int min;
                    int.TryParse(format.Substring(1).Trim(), out min);
                    return this.ToBytes(false).ToHexString(ch == 'X', min, trimZeros: true);
                }

                if (((ch != 'G') && (ch != 'g')) && ((ch != 'D') && (ch != 'd')))
                {
                    throw new NotSupportedException("Not supported format: " + format);
                }
            }

            return ToString((NumberFormatInfo) formatProvider.GetFormat(typeof (NumberFormatInfo)));
        }

        private string ToString(NumberFormatInfo info)
        {
            if (Sign == 0)
            {
                return "0";
            }

            var sb = new StringBuilder();
            var ten = new Int256(10);
            Int256 current = Sign < 0 ? -this : this;
            while (true)
            {
                Int256 r;
                current = DivRem(current, ten, out r);
                if (r._d > 0 || current.Sign != 0 || (sb.Length == 0))
                {
                    sb.Insert(0, (char) ('0' + r._d));
                }
                if (current.Sign == 0)
                {
                    break;
                }
            }

            string s = sb.ToString();
            if ((Sign < 0) && (s != "0"))
            {
                return info.NegativeSign + s;
            }

            return s;
        }

        /// <summary>
        ///     Converts the numeric value to an equivalent object. The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="conversionType">The target conversion type.</param>
        /// <param name="provider">An object that supplies culture-specific information about the conversion.</param>
        /// <param name="asLittleEndian">As little endian.</param>
        /// <param name="value">
        ///     When this method returns, contains the value that is equivalent to the numeric value, if the
        ///     conversion succeeded, or is null if the conversion failed. This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if this value was converted successfully; otherwise, false.</returns>
        public bool TryConvert(Type conversionType, IFormatProvider provider, bool asLittleEndian, out object value)
        {
            if (conversionType == typeof (bool))
            {
                value = (bool) this;
                return true;
            }

            if (conversionType == typeof (byte))
            {
                value = (byte) this;
                return true;
            }

            if (conversionType == typeof (char))
            {
                value = (char) this;
                return true;
            }

            if (conversionType == typeof (decimal))
            {
                value = (decimal) this;
                return true;
            }

            if (conversionType == typeof (double))
            {
                value = (double) this;
                return true;
            }

            if (conversionType == typeof (short))
            {
                value = (short) this;
                return true;
            }

            if (conversionType == typeof (int))
            {
                value = (int) this;
                return true;
            }

            if (conversionType == typeof (long))
            {
                value = (long) this;
                return true;
            }

            if (conversionType == typeof (sbyte))
            {
                value = (sbyte) this;
                return true;
            }

            if (conversionType == typeof (float))
            {
                value = (float) this;
                return true;
            }

            if (conversionType == typeof (string))
            {
                value = ToString(null, provider);
                return true;
            }

            if (conversionType == typeof (ushort))
            {
                value = (ushort) this;
                return true;
            }

            if (conversionType == typeof (uint))
            {
                value = (uint) this;
                return true;
            }

            if (conversionType == typeof (ulong))
            {
                value = (ulong) this;
                return true;
            }

            if (conversionType == typeof (byte[]))
            {
                value = this.ToBytes(asLittleEndian);
                return true;
            }

            if (conversionType == typeof (Guid))
            {
                value = new Guid(this.ToBytes(asLittleEndian));
                return true;
            }

            if (conversionType == typeof (Int128))
            {
                value = (Int128) this;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        ///     Converts the string representation of a number to its Int256 equivalent.
        /// </summary>
        /// <param name="value">A string that contains a number to convert.</param>
        /// <returns>
        ///     A value that is equivalent to the number specified in the value parameter.
        /// </returns>
        public static Int256 Parse(string value)
        {
            return Parse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        ///     Converts the string representation of a number in a specified style format to its Int256 equivalent.
        /// </summary>
        /// <param name="value">A string that contains a number to convert.</param>
        /// <param name="style">A bitwise combination of the enumeration values that specify the permitted format of value.</param>
        /// <returns>
        ///     A value that is equivalent to the number specified in the value parameter.
        /// </returns>
        public static Int256 Parse(string value, NumberStyles style)
        {
            return Parse(value, style, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        ///     Converts the string representation of a number in a culture-specific format to its Int256 equivalent.
        /// </summary>
        /// <param name="value">A string that contains a number to convert.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about value.</param>
        /// <returns>
        ///     A value that is equivalent to the number specified in the value parameter.
        /// </returns>
        public static Int256 Parse(string value, IFormatProvider provider)
        {
            return Parse(value, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        ///     Converts the string representation of a number in a specified style and culture-specific format to its Int256
        ///     equivalent.
        /// </summary>
        /// <param name="value">A string that contains a number to convert.</param>
        /// <param name="style">A bitwise combination of the enumeration values that specify the permitted format of value.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about value.</param>
        /// <returns>A value that is equivalent to the number specified in the value parameter.</returns>
        public static Int256 Parse(string value, NumberStyles style, IFormatProvider provider)
        {
            Int256 result;
            if (!TryParse(value, style, provider, out result))
            {
                throw new ArgumentException(null, "value");
            }

            return result;
        }

        /// <summary>
        ///     Tries to convert the string representation of a number to its Int256 equivalent, and returns a value that indicates
        ///     whether the conversion succeeded..
        /// </summary>
        /// <param name="value">The string representation of a number.</param>
        /// <param name="result">
        ///     When this method returns, contains the Int256 equivalent to the number that is contained in value,
        ///     or Int256.Zero if the conversion failed. This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        ///     true if the value parameter was converted successfully; otherwise, false.
        /// </returns>
        public static bool TryParse(string value, out Int256 result)
        {
            return TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        /// <summary>
        ///     Tries to convert the string representation of a number in a specified style and culture-specific format to its
        ///     Int256 equivalent, and returns a value that indicates whether the conversion succeeded..
        /// </summary>
        /// <param name="value">
        ///     The string representation of a number. The string is interpreted using the style specified by
        ///     style.
        /// </param>
        /// <param name="style">
        ///     A bitwise combination of enumeration values that indicates the style elements that can be present
        ///     in value. A typical value to specify is NumberStyles.Integer.
        /// </param>
        /// <param name="provider">An object that supplies culture-specific formatting information about value.</param>
        /// <param name="result">
        ///     When this method returns, contains the Int256 equivalent to the number that is contained in value,
        ///     or Int256.Zero if the conversion failed. This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if the value parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string value, NumberStyles style, IFormatProvider provider, out Int256 result)
        {
            result = Zero;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (value.StartsWith("x", StringComparison.OrdinalIgnoreCase))
            {
                style |= NumberStyles.AllowHexSpecifier;
                value = value.Substring(1);
            }
            else if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                style |= NumberStyles.AllowHexSpecifier;
                value = value.Substring(2);
            }

            if ((style & NumberStyles.AllowHexSpecifier) == NumberStyles.AllowHexSpecifier)
            {
                return TryParseHex(value, out result);
            }

            return TryParseNum(value, out result);
        }

        private static bool TryParseHex(string value, out Int256 result)
        {
            if (value.Length > 64)
            {
                throw new OverflowException();
            }

            result = Zero;
            int pos = 0;
            for (int i = value.Length - 1; i >= 0; i--)
            {
                char ch = value[i];
                ulong bch;
                if ((ch >= '0') && (ch <= '9'))
                {
                    bch = (ulong) (ch - '0');
                }
                else if ((ch >= 'A') && (ch <= 'F'))
                {
                    bch = (ulong) (ch - 'A' + 10);
                }
                else if ((ch >= 'a') && (ch <= 'f'))
                {
                    bch = (ulong) (ch - 'a' + 10);
                }
                else
                {
                    return false;
                }

                if (pos < 64)
                {
                    result._d |= bch << pos;
                }
                else if (pos < 128)
                {
                    result._c |= bch << pos;
                }
                else if (pos < 192)
                {
                    result._b |= bch << pos;
                }
                else if (pos < 256)
                {
                    result._a |= bch << pos;
                }
                pos += 4;
            }
            return true;
        }

        private static bool TryParseNum(string value, out Int256 result)
        {
            result = Zero;
            foreach (char ch in value)
            {
                byte b;
                if ((ch >= '0') && (ch <= '9'))
                {
                    b = (byte) (ch - '0');
                }
                else
                {
                    return false;
                }

                result = 10*result;
                result += b;
            }
            return true;
        }

        /// <summary>
        ///     Converts the value of this instance to an <see cref="T:System.Object" /> of the specified
        ///     <see cref="T:System.Type" /> that has an equivalent value, using the specified culture-specific formatting
        ///     information.
        /// </summary>
        /// <param name="conversionType">The <see cref="T:System.Type" /> to which the value of this instance is converted.</param>
        /// <param name="provider">
        ///     An <see cref="T:System.IFormatProvider" /> interface implementation that supplies
        ///     culture-specific formatting information.
        /// </param>
        /// <param name="asLittleEndian">As little endian.</param>
        /// <returns>
        ///     An <see cref="T:System.Object" /> instance of type <paramref name="conversionType" /> whose value is equivalent to
        ///     the value of this instance.
        /// </returns>
        public object ToType(Type conversionType, IFormatProvider provider, bool asLittleEndian)
        {
            object value;
            if (TryConvert(conversionType, provider, asLittleEndian, out value))
            {
                return value;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        ///     Compares the current instance with another object of the same type and returns an integer that indicates whether
        ///     the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        ///     A value that indicates the relative order of the objects being compared. The return value has these meanings: Value
        ///     Meaning Less than zero This instance is less than <paramref name="obj" />. Zero This instance is equal to
        ///     <paramref name="obj" />. Greater than zero This instance is greater than <paramref name="obj" />.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">
        ///     <paramref name="obj" /> is not the same type as this instance.
        /// </exception>
        int IComparable.CompareTo(object obj)
        {
            return Compare(this, obj);
        }

        /// <summary>
        ///     Compares two Int256 values and returns an integer that indicates whether the first value is less than, equal to, or
        ///     greater than the second value.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>A signed integer that indicates the relative values of left and right, as shown in the following table.</returns>
        public static int Compare(Int256 left, object right)
        {
            if (right is Int256)
            {
                return Compare(left, (Int256) right);
            }

            // NOTE: this could be optimized type per type
            if (right is bool)
            {
                return Compare(left, new Int256((bool) right));
            }

            if (right is byte)
            {
                return Compare(left, new Int256((byte) right));
            }

            if (right is char)
            {
                return Compare(left, new Int256((char) right));
            }

            if (right is decimal)
            {
                return Compare(left, new Int256((decimal) right));
            }

            if (right is double)
            {
                return Compare(left, new Int256((double) right));
            }

            if (right is short)
            {
                return Compare(left, new Int256((short) right));
            }

            if (right is int)
            {
                return Compare(left, new Int256((int) right));
            }

            if (right is long)
            {
                return Compare(left, new Int256((long) right));
            }

            if (right is sbyte)
            {
                return Compare(left, new Int256((sbyte) right));
            }

            if (right is float)
            {
                return Compare(left, new Int256((float) right));
            }

            if (right is ushort)
            {
                return Compare(left, new Int256((ushort) right));
            }

            if (right is uint)
            {
                return Compare(left, new Int256((uint) right));
            }

            if (right is ulong)
            {
                return Compare(left, new Int256((ulong) right));
            }

            var bytes = right as byte[];
            if ((bytes != null) && (bytes.Length == 32))
            {
                // TODO: ensure endian.
                return Compare(left, bytes.ToInt256(0));
            }

            if (right is Guid)
            {
                return Compare(left, new Int256((Guid) right));
            }

            throw new ArgumentException();
        }

        /// <summary>
        ///     Compares two 256-bit signed integer values and returns an integer that indicates whether the first value is less
        ///     than, equal to, or greater than the second value.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>
        ///     A signed number indicating the relative values of this instance and value.
        /// </returns>
        public static int Compare(Int256 left, Int256 right)
        {
            int leftSign = left.Sign;
            int rightSign = right.Sign;

            if (leftSign == 0 && rightSign == 0)
            {
                return 0;
            }

            if (leftSign >= 0 && rightSign < 0)
            {
                return 1;
            }

            if (leftSign < 0 && rightSign >= 0)
            {
                return -1;
            }

            if (left._a != right._a)
            {
                return left._a.CompareTo(right._a);
            }
            if (left._b != right._b)
            {
                return left._b.CompareTo(right._b);
            }
            if (left._c != right._c)
            {
                return left._c.CompareTo(right._c);
            }

            return left._d.CompareTo(right._d);
        }

        /// <summary>
        ///     Compares this instance to a specified 256-bit signed integer and returns an indication of their relative values.
        /// </summary>
        /// <param name="value">An integer to compare.</param>
        /// <returns>A signed number indicating the relative values of this instance and value.</returns>
        public int CompareTo(Int256 value)
        {
            return Compare(this, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Not()
        {
            _a = ~_a;
            _b = ~_b;
            _c = ~_c;
            _d = ~_d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Negate()
        {
            Not();
            this++;
        }

        /// <summary>
        ///     Negates a specified Int256 value.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <returns>The result of the value parameter multiplied by negative one (-1).</returns>
        public static Int256 Negate(Int256 value)
        {
            value.Negate();
            return value;
        }

        /// <summary>
        ///     Gets the absolute value this object.
        /// </summary>
        /// <returns>The absolute value.</returns>
        public Int256 ToAbs()
        {
            return Abs(this);
        }

        /// <summary>
        ///     Gets the absolute value of an Int256 object.
        /// </summary>
        /// <param name="value">A number.</param>
        /// <returns>
        ///     The absolute value.
        /// </returns>
        public static Int256 Abs(Int256 value)
        {
            if (value.Sign < 0)
            {
                return -value;
            }

            return value;
        }

        /// <summary>
        ///     Adds two Int256 values and returns the result.
        /// </summary>
        /// <param name="left">The first value to add.</param>
        /// <param name="right">The second value to add.</param>
        /// <returns>The sum of left and right.</returns>
        public static Int256 Add(Int256 left, Int256 right)
        {
            return left + right;
        }

        /// <summary>
        ///     Subtracts one Int256 value from another and returns the result.
        /// </summary>
        /// <param name="left">The value to subtract from (the minuend).</param>
        /// <param name="right">The value to subtract (the subtrahend).</param>
        /// <returns>The result of subtracting right from left.</returns>
        public static Int256 Subtract(Int256 left, Int256 right)
        {
            return left - right;
        }

        /// <summary>
        ///     Divides one Int256 value by another and returns the result.
        /// </summary>
        /// <param name="dividend">The value to be divided.</param>
        /// <param name="divisor">The value to divide by.</param>
        /// <returns>The quotient of the division.</returns>
        public static Int256 Divide(Int256 dividend, Int256 divisor)
        {
            Int256 integer;
            return DivRem(dividend, divisor, out integer);
        }

        /// <summary>
        ///     Performs integer division on two Int256 values and returns the remainder.
        /// </summary>
        /// <param name="dividend">The value to be divided.</param>
        /// <param name="divisor">The value to divide by.</param>
        /// <returns>The remainder after dividing dividend by divisor.</returns>
        public static Int256 Remainder(Int256 dividend, Int256 divisor)
        {
            Int256 remainder;
            DivRem(dividend, divisor, out remainder);
            return remainder;
        }

        /// <summary>
        ///     Divides one Int256 value by another, returns the result, and returns the remainder in an output parameter.
        /// </summary>
        /// <param name="dividend">The value to be divided.</param>
        /// <param name="divisor">The value to divide by.</param>
        /// <param name="remainder">
        ///     When this method returns, contains an Int256 value that represents the remainder from the
        ///     division. This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        ///     The quotient of the division.
        /// </returns>
        public static Int256 DivRem(Int256 dividend, Int256 divisor, out Int256 remainder)
        {
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }
            int dividendSign = dividend.Sign;
            dividend = dividendSign < 0 ? -dividend : dividend;
            int divisorSign = divisor.Sign;
            divisor = divisorSign < 0 ? -divisor : divisor;

            uint[] quotient;
            uint[] rem;
            MathUtils.DivModUnsigned(dividend.ToUIn32Array(), divisor.ToUIn32Array(), out quotient, out rem);
            remainder = new Int256(1, rem);
            return new Int256(dividendSign*divisorSign, quotient);
        }

        /// <summary>
        ///     Converts an Int256 value to an unsigned long array.
        /// </summary>
        /// <returns>
        ///     The value of the current Int256 object converted to an array of unsigned integers.
        /// </returns>
        public ulong[] ToUIn64Array()
        {
            return new[] {_d, _c, _b, _a};
        }

        /// <summary>
        ///     Converts an Int256 value to an unsigned integer array.
        /// </summary>
        /// <returns>The value of the current Int256 object converted to an array of unsigned integers.</returns>
        public uint[] ToUIn32Array()
        {
            var ints = new uint[8];
            ulong[] ulongs = ToUIn64Array();
            Buffer.BlockCopy(ulongs, 0, ints, 0, 32);
            return ints;
        }

        /// <summary>
        ///     Returns the product of two Int256 values.
        /// </summary>
        /// <param name="left">The first number to multiply.</param>
        /// <param name="right">The second number to multiply.</param>
        /// <returns>The product of the left and right parameters.</returns>
        public static Int256 Multiply(Int256 left, Int256 right)
        {
            int leftSign = left.Sign;
            left = leftSign < 0 ? -left : left;
            int rightSign = right.Sign;
            right = rightSign < 0 ? -right : right;

            uint[] xInts = left.ToUIn32Array();
            uint[] yInts = right.ToUIn32Array();
            var mulInts = new uint[16];

            for (int i = 0; i < xInts.Length; i++)
            {
                int index = i;
                ulong remainder = 0;
                foreach (uint yi in yInts)
                {
                    remainder = remainder + (ulong) xInts[i]*yi + mulInts[index];
                    mulInts[index++] = (uint) remainder;
                    remainder = remainder >> 32;
                }

                while (remainder != 0)
                {
                    remainder += mulInts[index];
                    mulInts[index++] = (uint) remainder;
                    remainder = remainder >> 32;
                }
            }
            return new Int256(leftSign*rightSign, mulInts);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Boolean" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int256(bool value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Byte" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int256(byte value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Char" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int256(char value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="System.Decimal" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator Int256(decimal value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="System.Double" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator Int256(double value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Int16" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int256(short value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Int32" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int256(int value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Int64" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int256(long value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.SByte" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int256(sbyte value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="System.Single" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator Int256(float value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.UInt16" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int256(ushort value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.UInt32" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int256(uint value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.UInt64" /> to <see cref="Int256" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int256(ulong value)
        {
            return new Int256(value);
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.Boolean" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator bool(Int256 value)
        {
            return value.Sign != 0;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.Byte" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator byte(Int256 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < byte.MinValue) || (value > byte.MaxValue))
            {
                throw new OverflowException();
            }

            return (byte) value._d;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.Char" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator char(Int256 value)
        {
            if (value.Sign == 0)
            {
                return (char) 0;
            }

            if ((value < char.MinValue) || (value > char.MaxValue))
            {
                throw new OverflowException();
            }

            return (char) (ushort) value._d;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.Decimal" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator decimal(Int256 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < (Int256) decimal.MinValue) || (value > (Int256) decimal.MaxValue))
            {
                throw new OverflowException();
            }

            return new decimal((int) (value._d & 0xFFFFFFFF), (int) (value._d >> 32), (int) (value._c & 0xFFFFFFFF), value.Sign < 0, 0);
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.Double" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator double(Int256 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            double d;
            NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;
            if (!double.TryParse(value.ToString(nfi), NumberStyles.Number, nfi, out d))
            {
                throw new OverflowException();
            }

            return d;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.Single" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator float(Int256 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            float f;
            NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;
            if (!float.TryParse(value.ToString(nfi), NumberStyles.Number, nfi, out f))
            {
                throw new OverflowException();
            }

            return f;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.Int16" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator short(Int256 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < short.MinValue) || (value > short.MaxValue))
            {
                throw new OverflowException();
            }

            return (short) value._d;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.Int32" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator int(Int256 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < int.MinValue) || (value > int.MaxValue))
            {
                throw new OverflowException();
            }

            return ((int) value._d);
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.Int64" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator long(Int256 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < long.MinValue) || (value > long.MaxValue))
            {
                throw new OverflowException();
            }

            return (long) value._d;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.UInt32" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator uint(Int256 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < uint.MinValue) || (value > uint.MaxValue))
            {
                throw new OverflowException();
            }

            return (uint) value._d;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.UInt16" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator ushort(Int256 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < ushort.MinValue) || (value > ushort.MaxValue))
            {
                throw new OverflowException();
            }

            return (ushort) value._d;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int256" /> to <see cref="System.UInt64" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator ulong(Int256 value)
        {
            if ((value < ushort.MinValue) || (value > ushort.MaxValue))
            {
                throw new OverflowException();
            }

            return value._d;
        }

        /// <summary>
        ///     Implements the operator &gt;.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator >(Int256 left, Int256 right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        ///     Implements the operator &lt;.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator <(Int256 left, Int256 right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        ///     Implements the operator &gt;=.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator >=(Int256 left, Int256 right)
        {
            return Compare(left, right) >= 0;
        }

        /// <summary>
        ///     Implements the operator &lt;=.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator <=(Int256 left, Int256 right)
        {
            return Compare(left, right) <= 0;
        }

        /// <summary>
        ///     Implements the operator !=.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator !=(Int256 left, Int256 right)
        {
            return Compare(left, right) != 0;
        }

        /// <summary>
        ///     Implements the operator ==.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator ==(Int256 left, Int256 right)
        {
            return Compare(left, right) == 0;
        }

        /// <summary>
        ///     Implements the operator +.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static Int256 operator +(Int256 value)
        {
            return value;
        }

        /// <summary>
        ///     Implements the operator -.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static Int256 operator -(Int256 value)
        {
            return Negate(value);
        }

        /// <summary>
        ///     Implements the operator +.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static Int256 operator +(Int256 left, Int256 right)
        {
            left._a += right._a;
            left._b += right._b;
            if (left._b < right._b)
            {
                left._a++;
            }
            left._c += right._c;
            if (left._c < right._c)
            {
                left._b++;
                if (left._b < left._b - 1)
                {
                    left._a++;
                }
            }
            left._d += right._d;
            if (left._d < right._d)
            {
                left._c++;
                if (left._c < left._c - 1)
                {
                    left._b++;
                    if (left._b < left._b - 1)
                    {
                        left._a++;
                    }
                }
            }

            return left;
        }

        /// <summary>
        ///     Implements the operator -.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static Int256 operator -(Int256 left, Int256 right)
        {
            return left + -right;
        }

        /// <summary>
        ///     Implements the operator %.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static Int256 operator %(Int256 dividend, Int256 divisor)
        {
            return Remainder(dividend, divisor);
        }

        /// <summary>
        ///     Implements the operator /.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static Int256 operator /(Int256 dividend, Int256 divisor)
        {
            return Divide(dividend, divisor);
        }

        /// <summary>
        ///     Implements the operator *.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static Int256 operator *(Int256 left, Int256 right)
        {
            return Multiply(left, right);
        }

        /// <summary>
        ///     Implements the operator &gt;&gt;.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="shift">The shift.</param>
        /// <returns>The result of the operator.</returns>
        public static Int256 operator >>(Int256 value, int shift)
        {
            if (shift == 0)
            {
                return value;
            }

            ulong[] bits = MathUtils.ShiftRightSigned(value.ToUIn64Array(), shift);
            value._a = bits[3];
            value._b = bits[2];
            value._c = bits[1];
            value._d = bits[0];     //lo is stored in array entry 0

            return value;
        }

        /// <summary>
        ///     Implements the operator &lt;&lt;.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="shift">The shift.</param>
        /// <returns>The result of the operator.</returns>
        public static Int256 operator <<(Int256 value, int shift)
        {
            if (shift == 0)
            {
                return value;
            }

            ulong[] bits = MathUtils.ShiftLeft(value.ToUIn64Array(), shift);
            value._a = bits[3];
            value._b = bits[2];
            value._c = bits[1];
            value._d = bits[0];     //lo is stored in array entry 0

            return value;
        }

        /// <summary>
        ///     Implements the operator |.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static Int256 operator |(Int256 left, Int256 right)
        {
            if (left == 0)
            {
                return right;
            }

            if (right == 0)
            {
                return left;
            }

            left._a |= right._a;
            left._b |= right._b;
            left._c |= right._c;
            left._d |= right._d;
            return left;
        }

        /// <summary>
        ///     Implements the operator &amp;.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static Int256 operator &(Int256 left, Int256 right)
        {
            if (left == 0 || right == 0)
            {
                return Zero;
            }

            left._a &= right._a;
            left._b &= right._b;
            left._c &= right._c;
            left._d &= right._d;
            return left;
        }

        /// <summary>
        ///     Implements the operator ~.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the operator.</returns>
        public static Int256 operator ~(Int256 value)
        {
            return new Int256(~value._a, ~value._b, ~value._c, ~value._d);
        }

        /// <summary>
        ///     Implements the operator ++.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the operator.</returns>
        public static Int256 operator ++(Int256 value)
        {
            return value + 1;
        }

        /// <summary>
        ///     Implements the operator --.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the operator.</returns>
        public static Int256 operator --(Int256 value)
        {
            return value - 1;
        }
    }
}
