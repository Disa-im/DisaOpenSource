using System;

namespace Disa.Framework
{
    public class ServiceBubbleSendFailedException : Exception
    {
        public ServiceBubbleSendFailedException(string message)
            : base(message)
        {
        }
    }
}