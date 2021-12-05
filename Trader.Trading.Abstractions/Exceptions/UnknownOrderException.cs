using Microsoft.Toolkit.Diagnostics;
using System.Runtime.Serialization;

namespace Outcompute.Trader.Trading.Exceptions;

[Serializable]
public class UnknownOrderException : TraderException
{
    public UnknownOrderException()
    {
    }

    public UnknownOrderException(string message) : base(message)
    {
    }

    public UnknownOrderException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected UnknownOrderException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
    {
        Guard.IsNotNull(serializationInfo, nameof(serializationInfo));

        Symbol = serializationInfo.GetString(nameof(Symbol)) ?? throw new SerializationException($"Could not deserialize value for property '{Symbol}'");
        OrderId = serializationInfo.GetInt64(nameof(OrderId));
    }

    public UnknownOrderException(string symbol, long orderId)
        : base($"Unknown order '{orderId}' for symbol '{symbol}'")
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        Symbol = symbol;
        OrderId = orderId;
    }

    public UnknownOrderException(string symbol, long orderId, Exception innerException)
        : base($"Unknown order '{orderId}' for symbol '{symbol}'", innerException)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        Symbol = symbol;
        OrderId = orderId;
    }

    public string Symbol { get; } = Empty;
    public long OrderId { get; }
}