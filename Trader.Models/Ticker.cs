using System;

namespace Outcompute.Trader.Models
{
    public record Ticker(
        string Symbol,
        decimal PriceChange,
        decimal PriceChangePercent,
        decimal WeightedAvgPrice,
        decimal PrevClosePrice,
        decimal LastPrice,
        decimal LastQty,
        decimal BidPrice,
        decimal AskPrice,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal Volume,
        decimal QuoteVolume,
        DateTime OpenTime,
        DateTime CloseTime,
        int FirstId,
        int LastId,
        int Count);
}