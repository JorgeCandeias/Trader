using System;
using System.Runtime.Serialization;

namespace Trader.Trading.Binance
{
    [Serializable]
    public class BinanceTooManyRequestsException : Exception
    {
        public TimeSpan RetryAfter { get; }

        public BinanceTooManyRequestsException(TimeSpan retryAfter)
            : this($"Retry after '{retryAfter}'")
        {
            RetryAfter = retryAfter;
        }

        public BinanceTooManyRequestsException()
        {
        }

        public BinanceTooManyRequestsException(string message) : base(message)
        {
        }

        public BinanceTooManyRequestsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BinanceTooManyRequestsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            RetryAfter = (TimeSpan)info.GetValue(nameof(RetryAfter), typeof(TimeSpan))!;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(RetryAfter), RetryAfter, typeof(TimeSpan));
        }
    }
}