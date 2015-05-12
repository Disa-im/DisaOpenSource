// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Exceptions.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpTL
{
    /// <summary>
    ///     TL serialization exception.
    /// </summary>
    public class TLSerializationException : Exception
    {
        public TLSerializationException()
        {
        }

        public TLSerializationException(string message) : base(message)
        {
        }

        public TLSerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    ///     Invalid TL constructor number exception.
    /// </summary>
    public class InvalidTLConstructorNumberException : TLSerializationException
    {
        public InvalidTLConstructorNumberException()
        {
        }

        public InvalidTLConstructorNumberException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Invalid TL-schema exception.
    /// </summary>
    public class InvalidTLSchemaException : Exception
    {
        public InvalidTLSchemaException()
        {
        }

        public InvalidTLSchemaException(string message) : base(message)
        {
        }

        public InvalidTLSchemaException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    ///     TL serializer not found exception.
    /// </summary>
    public class TLSerializerNotFoundException : TLSerializationException
    {
        public TLSerializerNotFoundException()
        {
        }

        public TLSerializerNotFoundException(string message) : base(message)
        {
        }

        public TLSerializerNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
