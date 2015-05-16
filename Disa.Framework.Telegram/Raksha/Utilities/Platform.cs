// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Platform.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Raksha.Utilities
{
    internal static class Platform
    {
        internal static readonly string NewLine = GetNewLine();

        internal static readonly Assembly ThisAssembly = typeof (Platform).GetTypeInfo().Assembly;

        private static string GetNewLine()
        {
            return Environment.NewLine;
        }

        internal static int CompareIgnoreCase(string a, string b)
        {
            return String.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetEnvironmentVariable(string variable)
        {
            return null;
        }

        internal static Exception CreateNotImplementedException(string message)
        {
            return new NotImplementedException(message);
        }


        internal static IList CreateArrayList()
        {
            return new List<object>();
        }

        internal static IList CreateArrayList(int capacity)
        {
            return new List<object>(capacity);
        }

        internal static IList CreateArrayList(ICollection collection)
        {
            IList result = new List<object>(collection.Count);
            foreach (object o in collection)
            {
                result.Add(o);
            }
            return result;
        }

        internal static IList CreateArrayList(IEnumerable collection)
        {
            IList result = new List<object>();
            foreach (object o in collection)
            {
                result.Add(o);
            }
            return result;
        }

        internal static IDictionary CreateHashtable()
        {
            return new Dictionary<object, object>();
        }

        internal static IDictionary CreateHashtable(int capacity)
        {
            return new Dictionary<object, object>(capacity);
        }

        internal static IDictionary CreateHashtable(IDictionary dictionary)
        {
            IDictionary result = new Dictionary<object, object>(dictionary.Count);
            foreach (DictionaryEntry entry in dictionary)
            {
                result.Add(entry.Key, entry.Value);
            }
            return result;
        }
    }
}
