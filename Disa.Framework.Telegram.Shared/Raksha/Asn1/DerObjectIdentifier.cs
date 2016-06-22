// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DerObjectIdentifier.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BigMath;
using Raksha.Math;

namespace Raksha.Asn1
{
    public class DerObjectIdentifier : Asn1Object
    {
        private static readonly Regex OidRegex = new Regex(@"\A[0-2](\.[0-9]+)+\z");

        private readonly string _identifier;

        /**
         * return an Oid from the passed in object
         *
         * @exception ArgumentException if the object cannot be converted.
         */

        public DerObjectIdentifier(string identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException("identifier");
            }
            if (!OidRegex.IsMatch(identifier))
            {
                throw new FormatException("string " + identifier + " not an OID");
            }

            _identifier = identifier;
        }

        internal DerObjectIdentifier(byte[] bytes) : this(MakeOidStringFromBytes(bytes))
        {
        }

        // TODO Change to ID?
        public string Id
        {
            get { return _identifier; }
        }

        public static DerObjectIdentifier GetInstance(object obj)
        {
            if (obj == null || obj is DerObjectIdentifier)
            {
                return (DerObjectIdentifier) obj;
            }

            throw new ArgumentException("illegal object in GetInstance: " + obj.GetType().Name, "obj");
        }

        /**
         * return an object Identifier from a tagged object.
         *
         * @param obj the tagged object holding the object we want
         * @param explicitly true if the object is meant to be explicitly
         *              tagged false otherwise.
         * @exception ArgumentException if the tagged object cannot
         *               be converted.
         */

        public static DerObjectIdentifier GetInstance(Asn1TaggedObject obj, bool explicitly)
        {
            return GetInstance(obj.GetObject());
        }

        public virtual DerObjectIdentifier Branch(string branchID)
        {
            return new DerObjectIdentifier(_identifier + "." + branchID);
        }

        private void WriteField(Stream outputStream, long fieldValue)
        {
            var result = new byte[9];
            int pos = 8;
            result[pos] = (byte) (fieldValue & 0x7f);
            while (fieldValue >= (1L << 7))
            {
                fieldValue >>= 7;
                result[--pos] = (byte) ((fieldValue & 0x7f) | 0x80);
            }
            outputStream.Write(result, pos, 9 - pos);
        }

        private void WriteField(Stream outputStream, BigInteger fieldValue)
        {
            int byteCount = (fieldValue.BitLength + 6)/7;
            if (byteCount == 0)
            {
                outputStream.WriteByte(0);
            }
            else
            {
                BigInteger tmpValue = fieldValue;
                var tmp = new byte[byteCount];
                for (int i = byteCount - 1; i >= 0; i--)
                {
                    tmp[i] = (byte) ((tmpValue.IntValue & 0x7f) | 0x80);
                    tmpValue = tmpValue.ShiftRight(7);
                }
                tmp[byteCount - 1] &= 0x7f;
                outputStream.Write(tmp, 0, tmp.Length);
            }
        }

        internal override void Encode(DerOutputStream derOut)
        {
            var tok = new OidTokenizer(_identifier);
            using (var bOut = new MemoryStream())
            {
                using (var dOut = new DerOutputStream(bOut))
                {
                    string token = tok.NextToken();
                    int first = int.Parse(token);

                    token = tok.NextToken();
                    int second = int.Parse(token);

                    WriteField(bOut, first*40 + second);

                    while (tok.HasMoreTokens)
                    {
                        token = tok.NextToken();
                        if (token.Length < 18)
                        {
                            WriteField(bOut, Int64.Parse(token));
                        }
                        else
                        {
                            WriteField(bOut, new BigInteger(token));
                        }
                    }

                    dOut.Dispose();

                    derOut.WriteEncoded(Asn1Tags.ObjectIdentifier, bOut.ToArray());
                }
            }
        }

        protected override int Asn1GetHashCode()
        {
            return _identifier.GetHashCode();
        }

        protected override bool Asn1Equals(Asn1Object asn1Object)
        {
            var other = asn1Object as DerObjectIdentifier;

            if (other == null)
            {
                return false;
            }

            return _identifier.Equals(other._identifier);
        }

        public override string ToString()
        {
            return _identifier;
        }

        private static string MakeOidStringFromBytes(byte[] bytes)
        {
            var objId = new StringBuilder();
            long value = 0;
            BigInteger bigValue = null;
            bool first = true;

            for (int i = 0; i != bytes.Length; i++)
            {
                int b = bytes[i];

                if (value < 0x80000000000000L)
                {
                    value = value*128 + (b & 0x7f);
                    if ((b & 0x80) == 0) // end of number reached
                    {
                        if (first)
                        {
                            switch ((int) value/40)
                            {
                                case 0:
                                    objId.Append('0');
                                    break;
                                case 1:
                                    objId.Append('1');
                                    value -= 40;
                                    break;
                                default:
                                    objId.Append('2');
                                    value -= 80;
                                    break;
                            }
                            first = false;
                        }

                        objId.Append('.');
                        objId.Append(value);
                        value = 0;
                    }
                }
                else
                {
                    if (bigValue == null)
                    {
                        bigValue = BigInteger.ValueOf(value);
                    }
                    bigValue = bigValue.ShiftLeft(7);
                    bigValue = bigValue.Or(BigInteger.ValueOf(b & 0x7f));
                    if ((b & 0x80) == 0)
                    {
                        objId.Append('.');
                        objId.Append(bigValue);
                        bigValue = null;
                        value = 0;
                    }
                }
            }

            return objId.ToString();
        }
    }
}
