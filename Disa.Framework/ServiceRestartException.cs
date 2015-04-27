using System;

namespace Disa.Framework
{
    public class ServiceRestartException : Exception
    {
        public ServiceRestartException(string message) : base(message)
        {
        }
    }
}

