// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLSchema.TL.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BigMath.Utils;

namespace SharpTL.Compiler
{
    public partial class TLSchema
    {
        private static readonly Regex TLSchemaPartsRegex = new Regex(@"(?<Types>[\w\W\r\n]*)---functions---(?<Functions>[\w\W\r\n]*)");
        private static readonly Regex ReturnRegex = new Regex(@"\s*(\r\n|\n\r|\r|\n)+\s*");

        private static readonly Regex DeclarationRegex =
            new Regex(
                @"[\s]*(?<Declaration>(?<CombinatorName>[\w-[#]]*)(?:\s|#(?<CombinatorNumber>[0-9a-fA-F]{1,8})\s*)?" +
                    @"(?<Parameters>[\w\W-[;=]]+?)??\s*?=\s*(?<TypeName>[\w\W]+?));");

        private static readonly Regex SingleLineCommentRegex = new Regex("//.*$", RegexOptions.Multiline);
        private static readonly Regex MultiLineCommentRegex = new Regex(@"/\*.*?\*/", RegexOptions.Singleline);

        public static string CompileFromTL(string tlSchemaText, CompilationParams compilationParams)
        {
            TLSchema schema = FromTL(tlSchemaText);
            return schema.Compile(compilationParams);
        }

        /// <summary>
        ///     Compile TL-schema to C# object model.
        /// </summary>
        /// <param name="schemaText">TL-schema.</param>
        /// <returns>Compiled TL-schema.</returns>
        public static TLSchema FromTL(string schemaText)
        {
            // TODO: implement.
            throw new NotImplementedException();

            var typesCache = new TLTypesBox();

            IEnumerable<TLCombinator> constructors = new List<TLCombinator>();
            IEnumerable<TLCombinator> methods = new List<TLCombinator>();

            schemaText = RemoveComments(schemaText);
            schemaText = RemoveNewlines(schemaText);

            Match partsMatch = TLSchemaPartsRegex.Match(schemaText);
            if (!partsMatch.Success)
            {
                throw new InvalidTLSchemaException();
            }
            string typesPart = partsMatch.Groups["Types"].Value.Trim();
            string functionsPart = partsMatch.Groups["Functions"].Value.Trim();

            foreach (Match declarationMatch in DeclarationRegex.Matches(typesPart))
            {
                string declarationText = declarationMatch.Groups["Declaration"].Value.Trim();
                uint combinatorNumber = Crc32.Compute(declarationText, Encoding.UTF8);

                var declaration = new TLCombinator(declarationMatch.Groups["CombinatorName"].Value.Trim());

                Group combinatorNumberMatch = declarationMatch.Groups["CombinatorNumber"];
                declaration.Number = combinatorNumberMatch.Success ? Convert.ToUInt32(combinatorNumberMatch.Value, 16) : combinatorNumber;

                Group parametersMatch = declarationMatch.Groups["Parameters"];
                if (parametersMatch.Success)
                {
                }
            }

            return new TLSchema(constructors, methods);
        }

        private static string RemoveComments(string text)
        {
            text = SingleLineCommentRegex.Replace(text, string.Empty);
            return MultiLineCommentRegex.Replace(text, string.Empty);
        }

        private static string RemoveNewlines(string text)
        {
            return ReturnRegex.Replace(text, " ");
        }
    }
}
