using System.Runtime.Serialization;

namespace Outcompute.Trader.Trading.Exceptions;

[Serializable]
public class StopLossWouldTriggerImmediatelyException : TraderException
{
    public StopLossWouldTriggerImmediatelyException()
    {
    }

    public StopLossWouldTriggerImmediatelyException(string message) : base(message)
    {
    }

    public StopLossWouldTriggerImmediatelyException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected StopLossWouldTriggerImmediatelyException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
    {
        Guard.IsNotNull(serializationInfo, nameof(serializationInfo));

        Symbol = serializationInfo.GetString(nameof(Symbol)) ?? Empty;
        OrderSide = (OrderSide)(serializationInfo.GetValue(nameof(OrderSide), typeof(OrderSide)) ?? OrderSide.None);
        OrderType = (OrderType)(serializationInfo.GetValue(nameof(OrderType), typeof(OrderType)) ?? OrderType.None);
        Quantity = serializationInfo.GetDecimal(nameof(Quantity));
        StopPrice = serializationInfo.GetDecimal(nameof(StopPrice));
        Price = serializationInfo.GetDecimal(nameof(Price));
    }

    public StopLossWouldTriggerImmediatelyException(string symbol, OrderSide orderSide, OrderType orderType, decimal quantity, decimal stopPrice, decimal price)
        : base($"{symbol} {orderType} {orderSide} for {quantity} at {price} with stop loss {stopPrice} would trigger immediately")
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        Symbol = symbol;
        OrderSide = orderSide;
        OrderType = orderType;
        Quantity = quantity;
        StopPrice = stopPrice;
        Price = price;
    }

    public StopLossWouldTriggerImmediatelyException(string symbol, OrderSide orderSide, OrderType orderType, decimal quantity, decimal stopPrice, decimal price, Exception innerException)
        : base($"{symbol} {orderType} {orderSide} for {quantity} at {price} with stop loss {stopPrice} would trigger immediately", innerException)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        Symbol = symbol;
        OrderSide = orderSide;
        OrderType = orderType;
        Quantity = quantity;
        StopPrice = stopPrice;
        Price = price;
    }

    public string Symbol { get; } = Empty;
    public OrderSide OrderSide { get; } = OrderSide.None;
    public OrderType OrderType { get; } = OrderType.None;
    public decimal Quantity { get; } = 0;
    public decimal StopPrice { get; } = 0;
    public decimal Price { get; } = 0;

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue(nameof(Symbol), Symbol);
        info.AddValue(nameof(OrderSide), OrderSide);
        info.AddValue(nameof(OrderType), OrderType);
        info.AddValue(nameof(Quantity), Quantity);
        info.AddValue(nameof(StopPrice), StopPrice);
        info.AddValue(nameof(Price), Price);
    }
}