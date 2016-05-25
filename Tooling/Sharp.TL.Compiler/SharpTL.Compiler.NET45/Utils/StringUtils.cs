// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringUtils.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpTL.Compiler.Utils
{
    public static class StringUtils
    {
        /// <summary>
        ///     Converts a text to specified convention.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="cases">The cases.</param>
        /// <returns>string</returns>
        public static string ToConventionalCase(this string text, Case cases)
        {
            text = Regex.Replace(text, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");
            string[] splittedPhrase = text.Split(' ', '-', '.', '_');
            var sb = new StringBuilder();

            bool isCamelCase = cases == Case.CamelCase;
            foreach (String s in splittedPhrase)
            {
                char[] splittedPhraseChars = s.ToCharArray();
                if (splittedPhraseChars.Length > 0)
                {
                    char c = splittedPhraseChars[0];
                    splittedPhraseChars[0] = isCamelCase ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c);
                    isCamelCase = false;
                }
                sb.Append(splittedPhraseChars);
            }
            return sb.ToString();
        }
    }

    public enum Case
    {
        PascalCase,
        CamelCase
    }
}
