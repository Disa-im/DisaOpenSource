// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompilationParams.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace SharpTL.Compiler
{
    public class CompilationParams
    {
        public CompilationParams(string ns, string methodsInterfaceName = null)
        {
            Namespace = ns;
            MethodsInterfaceName = methodsInterfaceName ?? ns.Replace(".", string.Empty);
        }

        public string Namespace { get; set; }
        public string MethodsInterfaceName { get; set; }
    }
}
