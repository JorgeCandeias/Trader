using System;
using System.Runtime.Serialization;

namespace Outcompute.Trader.Trading.Algorithms.Exceptions
{
    [Serializable]
    public class AlgorithmNotInitializedException : AlgorithmException
    {
        public AlgorithmNotInitializedException()
        {
        }

        public AlgorithmNotInitializedException(string? message) : base(message)
        {
        }

        public AlgorithmNotInitializedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected AlgorithmNotInitializedException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}