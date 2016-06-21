// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Strings.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace Raksha.Utilities
{
    /// <summary>
    ///     General string utilities.
    /// </summary>
    public static class Strings
    {
        internal static bool IsOneOf(string s, params string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (s == candidate)
                {
                    return true;
                }
            }
            return false;
        }

        public static string FromByteArray(byte[] bs)
        {
            var cs = new char[bs.Length];
            for (int i = 0; i < cs.Length; ++i)
            {
                cs[i] = Convert.ToChar(bs[i]);
            }
            return new string(cs);
        }

        public static byte[] ToByteArray(char[] cs)
        {
            var bs = new byte[cs.Length];
            for (int i = 0; i < bs.Length; ++i)
            {
                bs[i] = Convert.ToByte(cs[i]);
            }
            return bs;
        }

        public static byte[] ToByteArray(string s)
        {
            var bs = new byte[s.Length];
            for (int i = 0; i < bs.Length; ++i)
            {
                bs[i] = Convert.ToByte(s[i]);
            }
            return bs;
        }

        public static string FromAsciiByteArray(byte[] bytes)
        {
#if SILVERLIGHT || PCL
            // TODO Check for non-ASCII bytes in input?
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
#else
            return Encoding.ASCII.GetString(bytes, 0, bytes.Length);
#endif
        }

        public static byte[] ToAsciiByteArray(char[] cs)
        {
#if SILVERLIGHT || PCL
            // TODO Check for non-ASCII characters in input?
            return Encoding.UTF8.GetBytes(cs);
#else
            return Encoding.ASCII.GetBytes(cs);
#endif
        }

        public static byte[] ToAsciiByteArray(string s)
        {
#if SILVERLIGHT || PCL
            // TODO Check for non-ASCII characters in input?
            return Encoding.UTF8.GetBytes(s);
#else
            return Encoding.ASCII.GetBytes(s);
#endif
        }

        public static string FromUtf8ByteArray(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static byte[] ToUtf8ByteArray(char[] cs)
        {
            return Encoding.UTF8.GetBytes(cs);
        }

        public static byte[] ToUtf8ByteArray(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }
    }
}
