// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLShemaCompilerFacts.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace SharpTL.Compiler.Tests
{
    [TestFixture]
    public class TLShemaCompilerFacts
    {
        private static string GetTestTLSchema()
        {
            return File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TL-schemas", "Test.tl"));
        }

        private static string GetTestTLJsonSchema()
        {
            return File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TL-schemas", "TestTLSchema.json"));
        }

        [Test]
        public void Should_compile_TL_schema()
        {
            string sharpTLSchemaCode = TLSchema.CompileFromJson(GetTestTLJsonSchema(), new CompilationParams("SharpTL.TestNamespace"));
            sharpTLSchemaCode.Should().NotBeNullOrEmpty();
        }
    }
}
