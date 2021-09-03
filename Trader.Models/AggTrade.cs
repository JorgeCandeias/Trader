using System;

namespace Outcompute.Trader.Models
{
    public record AggTrade(
        int AggregateTradeId,
        decimal Price,
        decimal Quantity,
        int FirstTradeId,
        int LastTradeId,
        DateTime Timestamp,
        bool IsBuyerMaker,
        bool IsBestMatch);
}