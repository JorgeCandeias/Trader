using System;

namespace Outcompute.Trader.Data.Sql.Models
{
    internal record TickerEntity(
        string Symbol,
        DateTime EventTime,
        decimal ClosePrice,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal AssetVolume,
        decimal QuoteVolume);
}