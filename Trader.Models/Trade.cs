using System;

namespace Trader.Models
{
    public record Trade(
        int Id,
        decimal Price,
        decimal Quantity,
        decimal QuoteQuantity,
        DateTime Time,
        bool IsBuyerMaker,
        bool IsBestMatch);
}