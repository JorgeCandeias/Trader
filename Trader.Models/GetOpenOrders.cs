namespace Outcompute.Trader.Models;

public record GetOpenOrders(
    string Symbol,
    TimeSpan? ReceiveWindow,
    DateTime Timestamp);