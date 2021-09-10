using System;
using System.Runtime.Serialization;

namespace Outcompute.Trader.Trading.Watchdog
{
    [Serializable]
    public class GrainWatchdogException : Exception
    {
        public GrainWatchdogException()
        {
        }

        public GrainWatchdogException(string message) : base(message)
        {
        }

        public GrainWatchdogException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GrainWatchdogException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base()
        {
        }
    }
}