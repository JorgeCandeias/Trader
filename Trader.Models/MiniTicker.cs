using Orleans.Concurrency;
using System;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record MiniTicker(
        string Symbol,
        DateTime EventTime,
        decimal ClosePrice,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal AssetVolume,
        decimal QuoteVolume);
}