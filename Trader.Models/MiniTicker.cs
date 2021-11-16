using Orleans.Concurrency;

namespace Outcompute.Trader.Models;

[Immutable]
public record MiniTicker(
    string Symbol,
    DateTime EventTime,
    decimal ClosePrice,
    decimal OpenPrice,
    decimal HighPrice,
    decimal LowPrice,
    decimal AssetVolume,
    decimal QuoteVolume)
{
    public static MiniTicker Empty { get; } = new MiniTicker(string.Empty, DateTime.MinValue, 0m, 0m, 0m, 0m, 0m, 0m);
}