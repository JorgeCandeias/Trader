using System;

namespace Trader.Models
{
    public record Kline(
        string Symbol,
        KlineInterval Interval,
        DateTime OpenTime,
        DateTime CloseTime,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal ClosePrice,
        decimal Volume,
        decimal QuoteAssetVolume,
        int TradeCount,
        decimal TakerBuyBaseAssetVolume,
        decimal TakerBuyQuoteAssetVolume);
}