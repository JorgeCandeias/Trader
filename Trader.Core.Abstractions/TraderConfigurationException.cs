using System;
using System.Runtime.Serialization;

namespace Trader.Core
{
    [Serializable]
    public class TraderConfigurationException : Exception
    {
        public TraderConfigurationException()
        {
        }

        public TraderConfigurationException(string? message) : base(message)
        {
        }

        public TraderConfigurationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected TraderConfigurationException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}