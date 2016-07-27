using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public abstract class BubbleTransfer
    {
        public Action<int> Progress;
        public abstract Task Start();
    }
}