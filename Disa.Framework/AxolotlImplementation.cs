using System;

namespace Disa.Framework
{
    public class AxolotlImplementation
    {
        public Func<Axolotl.IAxolotl> InstantianteAxolotl { internal get; set; }
        public Axolotl.IAxolotlStatic AxolotlStatic { internal get; set; }
    }
}

