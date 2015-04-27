using System;

namespace Disa.Framework
{
    public class ServiceExpiredException : Exception
    {
        public ServiceExpiredException(string message)
            : base(message)
        {
        }
    }
}