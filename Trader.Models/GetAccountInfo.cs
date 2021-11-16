namespace Outcompute.Trader.Models;

public record GetAccountInfo(
    TimeSpan? ReceiveWindow,
    DateTime Timestamp);