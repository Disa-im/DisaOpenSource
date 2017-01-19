using System;

namespace Disa.Framework
{
    [DisaFramework]
    public interface ITerminal
    {
        void DoCommand(string[] args);
    }
}

