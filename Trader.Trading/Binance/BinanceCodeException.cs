using System;
using System.Net;
using System.Runtime.Serialization;

namespace Trader.Trading.Binance
{
    [Serializable]
    public class BinanceCodeException : BinanceException
    {
        public BinanceCodeException(int binanceCode, string message, HttpStatusCode statusCode) : base(message)
        {
            BinanceCode = binanceCode;
            StatusCode = statusCode;
        }

        public BinanceCodeException()
        {
        }

        public BinanceCodeException(string? message) : base(message)
        {
        }

        public BinanceCodeException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BinanceCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            BinanceCode = info.GetValue(nameof(BinanceCode), typeof(int)) is int binanceCode ? binanceCode : 0;
            StatusCode = info.GetValue(nameof(StatusCode), typeof(HttpStatusCode?)) is HttpStatusCode statusCode ? statusCode : HttpStatusCode.OK;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(BinanceCode), BinanceCode);
            info.AddValue(nameof(StatusCode), StatusCode);
        }

        public int BinanceCode { get; }
        public HttpStatusCode StatusCode { get; }
    }
}