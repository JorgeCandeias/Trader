using System.Net;
using System.Runtime.Serialization;

namespace Outcompute.Trader.Trading.Binance;

[Serializable]
public class BinanceCodeException : BinanceException
{
    public BinanceCodeException(int binanceCode, string binanceMessage, HttpStatusCode statusCode) : base($"{binanceCode}:{binanceMessage}")
    {
        Guard.IsNotNull(binanceMessage, nameof(binanceMessage));

        BinanceCode = binanceCode;
        BinanceMessage = binanceMessage;
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
        BinanceCode = info.GetInt32(nameof(BinanceCode));
        BinanceMessage = info.GetString(nameof(BinanceMessage))!;
        StatusCode = (HttpStatusCode)info.GetValue(nameof(StatusCode), typeof(HttpStatusCode))!;
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue(nameof(BinanceCode), BinanceCode);
        info.AddValue(nameof(StatusCode), StatusCode);
    }

    public int BinanceCode { get; }
    public string BinanceMessage { get; } = Empty;
    public HttpStatusCode StatusCode { get; } = HttpStatusCode.OK;
}