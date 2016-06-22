// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionUtilities.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace Raksha.Utilities.Collections
{
    public static class CollectionUtilities
    {
        public static void AddRange(IList to, ICollection range)
        {
            foreach (object o in range)
            {
                to.Add(o);
            }
        }

        public static bool CheckElementsAreOfType(IEnumerable e, Type t)
        {
            foreach (object o in e)
            {
                if (!t.GetTypeInfo().IsAssignableFrom(o.GetType().GetTypeInfo()))
                {
                    return false;
                }
            }
            return true;
        }

        public static IDictionary ReadOnly(IDictionary d)
        {
            return new UnmodifiableDictionaryProxy(d);
        }

        public static IList ReadOnly(IList l)
        {
            return new UnmodifiableListProxy(l);
        }

        public static ISet ReadOnly(ISet s)
        {
            return new UnmodifiableSetProxy(s);
        }

        public static string ToString(IEnumerable c)
        {
            var sb = new StringBuilder("[");

            IEnumerator e = c.GetEnumerator();

            if (e.MoveNext())
            {
                sb.Append(e.Current);

                while (e.MoveNext())
                {
                    sb.Append(", ");
                    sb.Append(e.Current);
                }
            }

            sb.Append(']');

            return sb.ToString();
        }
    }
}
