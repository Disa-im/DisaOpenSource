using System;

namespace Disa.Framework
{
    public class ServiceSpecialRestartException : Exception
    {
        public ServiceSpecialRestartException(string message)
            : base(message)
        {


        }
    }
}