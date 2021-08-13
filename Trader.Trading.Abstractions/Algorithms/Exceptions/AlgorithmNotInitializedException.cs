using System;
using System.Runtime.Serialization;

namespace Trader.Trading.Algorithms.Exceptions
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

        protected AlgorithmNotInitializedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
        }
    }
}