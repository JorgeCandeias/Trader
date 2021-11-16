namespace Outcompute.Trader.Models;

public record CancelAllOrders(
    string Symbol,
    TimeSpan? ReceiveWindow,
    DateTime Timestamp);