using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace BOG.Pathway.Client
{
    public class InvalidPayloadException : Exception, ISerializable
    {
        public InvalidPayloadException() : base()
        {

        }

        public InvalidPayloadException(string message) : base(message)
        {

        }

        public InvalidPayloadException(string message, Exception innerException) : base(message,innerException)
        {

        }

        public InvalidPayloadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
