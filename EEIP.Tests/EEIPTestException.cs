using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Sres.Net.EEIP.Tests
{
    public class EEIPTestException : Exception
    {
        public EEIPTestException()
        {
        }

        public EEIPTestException(string? message) : base(message)
        {
        }

        public EEIPTestException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected EEIPTestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
