using System;

namespace Disa.Framework
{
    public class ServiceBubbleGroupAddressException : Exception
    {
        public string Address { get; private set; }

        public ServiceBubbleGroupAddressException(string address)
        {
            Address = address;
        }
    }
}

