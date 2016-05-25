// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLSchema.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using BigMath;
using SharpTL.Serializers;

namespace SharpTL.Compiler
{
    /// <summary>
    ///     TL-schema.
    /// </summary>
    public partial class TLSchema
    {
        private readonly List<TLCombinator> _constructors;
        private readonly List<TLCombinator> _methods;
        private readonly TLTypesBox _typesBox = new TLTypesBox();

        private ReadOnlyCollection<TLCombinator> _roConstructors;
        private ReadOnlyCollection<TLCombinator> _roMethods;

        private static readonly Regex VectorRegex = new Regex(@"^(?:(?<Boxed>V)|(?<Bare>v))ector<(?<ItemsType>%?\w[\w\W-[\s]]*)>$", RegexOptions.Compiled);
        private static readonly Regex BareTypeRegex = new Regex(@"^%(?<Type>\w+)$", RegexOptions.Compiled);
        private static readonly Regex BoolRegex = new Regex(@"^Bool$", RegexOptions.Compiled);
        private static readonly Regex StringRegex = new Regex(@"^string$", RegexOptions.Compiled);
        private static readonly Regex DoubleRegex = new Regex(@"^double", RegexOptions.Compiled);
        private static readonly Regex Int32Regex = new Regex(@"^int$", RegexOptions.Compiled);
        private static readonly Regex Int64Regex = new Regex(@"^long$", RegexOptions.Compiled);
        private static readonly Regex Int128Regex = new Regex(@"^int128$", RegexOptions.Compiled);
        private static readonly Regex Int256Regex = new Regex(@"^int256$", RegexOptions.Compiled);
        private static readonly Regex TLBytesRegex = new Regex(@"^bytes$", RegexOptions.Compiled);
        private static readonly Regex TLObjectRegex = new Regex(@"^(Object|X|!X)$", RegexOptions.Compiled);

        private TLSchema(IEnumerable<TLCombinator> constructors, IEnumerable<TLCombinator> methods)
        {
            _constructors = new List<TLCombinator>(constructors);
            _methods = new List<TLCombinator>(methods);

            UpdateTypes();
        }

        public ReadOnlyCollection<TLCombinator> Constructors
        {
            get { return _roConstructors ?? (_roConstructors = new ReadOnlyCollection<TLCombinator>(_constructors)); }
        }

        public ReadOnlyCollection<TLCombinator> Methods
        {
            get { return _roMethods ?? (_roMethods = new ReadOnlyCollection<TLCombinator>(_methods)); }
        }

        public TLTypesBox TypesBox
        {
            get { return _typesBox; }
        }

        public static TLSchema Build(TLSchemaSourceType sourceType, string schemaText)
        {
            switch (sourceType)
            {
                case TLSchemaSourceType.TL:
                    return FromTL(schemaText);
                case TLSchemaSourceType.JSON:
                    return FromJson(schemaText);
                default:
                    throw new ArgumentOutOfRangeException("sourceType");
            }
        }

        public static string Compile(TLSchemaSourceType sourceType, string schemaText, CompilationParams compilationParams)
        {
            switch (sourceType)
            {
                case TLSchemaSourceType.TL:
                    return CompileFromTL(schemaText, compilationParams);
                case TLSchemaSourceType.JSON:
                    return CompileFromJson(schemaText, compilationParams);
                default:
                    throw new ArgumentOutOfRangeException("sourceType");
            }
        }

        public string Compile(CompilationParams compilationParams)
        {
            var template =
                new SharpTLDefaultTemplate(new TemplateVars
                {
                    Schema = this,
                    Namespace = compilationParams.Namespace,
                    MethodsInterfaceName = compilationParams.MethodsInterfaceName
                });
            return template.TransformText();
        }

        public string CompileMethodsImpl(CompilationParams compilationParams)
        {
            var template =
                new SchemaMethodsImplTemplate(new TemplateVars
                {
                    Schema = this,
                    Namespace = compilationParams.Namespace,
                    MethodsInterfaceName = compilationParams.MethodsInterfaceName
                });
            return template.TransformText();
        }

        public void UpdateTypes()
        {
            // Update types constructors.
            foreach (TLCombinator constructor in _constructors)
            {
                constructor.Type.Constructors.Clear();
            }
            foreach (TLCombinator constructor in _constructors)
            {
                TLType type = constructor.Type;
                if (!type.Constructors.Contains(constructor))
                {
                    type.Constructors.Add(constructor);
                }
            }

            // Update TLTypesBox.
            _typesBox.Clear();
            foreach (TLCombinator combinator in _constructors.Union(_methods))
            {
                _typesBox.Add(combinator.Type);
                foreach (TLCombinatorParameter parameter in combinator.Parameters)
                {
                    _typesBox.Add(parameter.Type);
                }
            }

			var allTypes = _typesBox.GetAll();
            // Fix types.
            foreach (TLType tlType in _typesBox.GetAll())
            {
                FixType(tlType);
            }

//			foreach (TLCombinator method in _methods)
//			{
//				foreach (TLCombinatorParameter param in method.Parameters) {
//					FixType(param.Type);
//				}
//			}
//
//			foreach (TLCombinator constructor in _constructors)
//			{
//				foreach (TLCombinatorParameter param in constructor.Parameters) {
//					FixType(param.Type);
//				}
//			}


            // Fix void returns.
            foreach (TLCombinator method in _methods.Where(method => !method.Type.HasConstructors && !method.Type.IsBuiltIn))
            {
                method.Type = _typesBox["void"];
            }


            // When property name equals class name, they must not be equal.
            foreach (TLCombinator constructor in _constructors)
            {
                foreach (TLCombinatorParameter parameter in constructor.Parameters)
                {
                    if (parameter.Name == constructor.Name)
                    {
                        // TODO: give more pretty name.
                        parameter.Name += "Property";
                    }
                }
            }
        }

        private static string GetBuiltInTypeName(uint constructorNumber)
        {
            return (from serializer in BuiltIn.BaseTypeSerializers
                let singleConstructorSerializer = serializer as ITLSingleConstructorSerializer
                let multiConstructorSerializer = serializer as ITLMultiConstructorSerializer
                where
                    (singleConstructorSerializer != null && constructorNumber == singleConstructorSerializer.ConstructorNumber) ||
                        multiConstructorSerializer != null && multiConstructorSerializer.ConstructorNumbers.Contains(constructorNumber)
                select serializer.SupportedType.FullName).FirstOrDefault();
        }

        private static bool HasBuiltInSerializer(uint constructorNumber)
        {
            return GetBuiltInTypeName(constructorNumber) != null;
        }

        private void FixType(TLType type)
        {
            // TODO: refactor this huge method.

            string typeName = type.OriginalName;

			//Additions for compatibity with layer 51 schema

            // Vector.
            Match match = VectorRegex.Match(typeName);
            if (match.Success)
            {
                TLType itemsType = _typesBox[match.Groups["ItemsType"].Value];
                FixType(itemsType);
                type.Name = string.Format("System.Collections.Generic.List<{0}>", itemsType.Name);
                if (match.Groups["Bare"].Success)
                {
                    type.SerializationModeOverride = TLSerializationMode.Bare;
                }
                type.IsBuiltIn = true;
                return;
            }

            // bool.
            match = BoolRegex.Match(typeName);
            if (match.Success)
            {
                type.Name = typeof (bool).FullName;
                type.IsBuiltIn = true;
                return;
            }

            // string.
            match = StringRegex.Match(typeName);
            if (match.Success)
            {
                type.Name = typeof (string).FullName;
                type.IsBuiltIn = true;
                return;
            }

            // string.
            match = DoubleRegex.Match(typeName);
            if (match.Success)
            {
                type.Name = typeof (double).FullName;
                type.IsBuiltIn = true;
                return;
            }

            // int.
            match = Int32Regex.Match(typeName);
            if (match.Success)
            {
                type.Name = typeof (UInt32).FullName;
                type.IsBuiltIn = true;
                return;
            }

            // long.
            match = Int64Regex.Match(typeName);
            if (match.Success)
            {
                type.Name = typeof (UInt64).FullName;
                type.IsBuiltIn = true;
                return;
            }

            // int128.
            match = Int128Regex.Match(typeName);
            if (match.Success)
            {
                type.Name = typeof (Int128).FullName;
                type.IsBuiltIn = true;
                return;
            }

            // int256.
            match = Int256Regex.Match(typeName);
            if (match.Success)
            {
                type.Name = typeof (Int256).FullName;
                type.IsBuiltIn = true;
                return;
            }

            // bytes.
            match = TLBytesRegex.Match(typeName);
            if (match.Success)
            {
                type.Name = typeof (byte[]).FullName;
                type.IsBuiltIn = true;
                return;
            }

            // % bare types.
            match = BareTypeRegex.Match(typeName);
            if (match.Success)
            {
                typeName = match.Groups["Type"].Value;
                type.Name = _constructors.Where(c => c.Type.Name == typeName).Select(c => c.Name).SingleOrDefault() ?? typeName;
                type.SerializationModeOverride = TLSerializationMode.Bare;
                // TODO: fix type.
                return;
            }

            // Object.
            match = TLObjectRegex.Match(typeName);
            if (match.Success)
            {
                type.Name = typeof (Object).FullName;
                type.IsBuiltIn = true;
            }
        }
    }
}
