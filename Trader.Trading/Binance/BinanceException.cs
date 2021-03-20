using System;
using System.Runtime.Serialization;

namespace Trader.Trading.Binance
{
    [Serializable]
    public class BinanceException : Exception
    {
        public BinanceException()
        {
        }

        public BinanceException(string? message) : base(message)
        {
        }

        public BinanceException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BinanceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}