// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLSchema.Json.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace SharpTL.Compiler
{
    public partial class TLSchema
    {
        public static TLSchema FromJson(string schemaText)
        {
            var typesBox = new TLTypesBox();

            JsonObject tlSchemaJsonObject = JsonObject.Parse(schemaText);

            IEnumerable<TLCombinator> constructors = CreateConstructorsFromJsonArrayObjects(tlSchemaJsonObject.ArrayObjects("constructors"), typesBox);
            IEnumerable<TLCombinator> methods = CreateMethodsFromJsonArrayObjects(tlSchemaJsonObject.ArrayObjects("methods"), typesBox);

            return new TLSchema(constructors, methods);
        }

        public static string CompileFromJson(string json, CompilationParams compilationParams)
        {
            TLSchema schema = FromJson(json);
            return schema.Compile(compilationParams);
        }

        private static IEnumerable<TLCombinator> CreateConstructorsFromJsonArrayObjects(JsonArrayObjects objects, TLTypesBox typesBox)
        {
            return CreateCombinatorsFromJsonArrayObjects(objects, "predicate", typesBox);
        }

        private static IEnumerable<TLCombinator> CreateMethodsFromJsonArrayObjects(JsonArrayObjects objects, TLTypesBox typesBox)
        {
            return CreateCombinatorsFromJsonArrayObjects(objects, "method", typesBox);
        }

        private static IEnumerable<TLCombinator> CreateCombinatorsFromJsonArrayObjects(JsonArrayObjects objects, string nameKey, TLTypesBox typesBox)
        {
            return
                objects.ConvertAll(
                    x =>
                        new TLCombinator(x.Get(nameKey))
                        {
                            Number = (uint) x.JsonTo<int>("id"),
                            Parameters =
                                x.ArrayObjects("params")
								.ConvertAll(param => new TLCombinatorParameter(param.Get("name"),param.Get("type")) {Type = typesBox[param.Get("type")]}),
                            Type = typesBox[x.Get("type")]
                        }).Where(combinator => !HasBuiltInSerializer(combinator.Number)).ToList();
        }
    }
}
