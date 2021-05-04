using System;
using System.Runtime.Serialization;

namespace Trader.Trading.Binance
{
    [Serializable]
    public class BinanceTooManyRequestsException : Exception
    {
        public DateTime RetryAfterUtc { get; }

        public BinanceTooManyRequestsException(DateTime retryAfterUtc)
            : this($"Retry after '{retryAfterUtc}'")
        {
            RetryAfterUtc = retryAfterUtc;
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
            RetryAfterUtc = info.GetDateTime(nameof(RetryAfterUtc));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(RetryAfterUtc), RetryAfterUtc);
        }
    }
}