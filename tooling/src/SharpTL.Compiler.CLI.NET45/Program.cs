// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using PowerArgs;
using SharpTL.Compiler.Annotations;

namespace SharpTL.Compiler.CLI
{
    [ArgExample(@"SharpTL.Compiler.CLI compile json schema1.json schema1.cs SomeSchema SchemaMethods", "Compile TL-schema described in JSON file."), UsedImplicitly]
    public class CompilerArgs
    {
        [ArgRequired]
        [ArgPosition(0)]
        public string Action { get; set; }

        public CompileArgs CompileArgs { get; set; }

        public static void Compile(CompileArgs args)
        {
            Console.WriteLine("Compiling...");
            string source = File.ReadAllText(args.Source);
            string outputSchemaFileName = string.Format("{0}.cs", args.Namespace);
            string outputSchemaMethodsImplFileName = string.Format("{0}.MethodsImpl.cs", args.Namespace);

            var compilationParams = new CompilationParams(args.Namespace, args.MethodsInterfaceName);
            
            TLSchema schema = TLSchema.Build(args.SourceType, source);
            
            string compiledSchema = schema.Compile(compilationParams);
            File.WriteAllText(outputSchemaFileName, compiledSchema);

            if (args.IncludeMethodsImplementation)
            {
                string compiledSchemaMethodsImpl = schema.CompileMethodsImpl(compilationParams);
                File.WriteAllText(outputSchemaMethodsImplFileName, compiledSchemaMethodsImpl);
            }

            Console.WriteLine("Compilation done successfully.");
        }
    }

    [UsedImplicitly]
    public class CompileArgs
    {
        [ArgShortcut("t")]
        [ArgRequired]
        [ArgDescription("The type of the TL schema.")]
        public TLSchemaSourceType SourceType { get; set; }

        [ArgShortcut("s")]
        [ArgRequired]
        [ArgDescription("The path to schema file.")]
        [ArgExistingFile]
        public string Source { get; set; }

        [ArgRequired(PromptIfMissing=true)]
        [ArgShortcut("ns")]
        [DefaultValue("TLSchema")]
        [ArgDescription("Namespace for compiled C# code.")]
        public string Namespace { get; set; }

        [ArgShortcut("mn")]
        [ArgDescription("Methods interface name.")]
        public string MethodsInterfaceName { get; set; }

        [ArgShortcut("impl")]
        [ArgDescription("Generate file with schema methods implementation.")]
        public bool IncludeMethodsImplementation { get; set; }
    }

    internal static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var parsed = Args.InvokeAction<CompilerArgs>(args);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                ArgUsage.GenerateUsageFromTemplate<CompilerArgs>().Write();
            }
        }
    }
}
