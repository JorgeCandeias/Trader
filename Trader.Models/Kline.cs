using System;

namespace Trader.Models
{
    public record Kline(
        DateTime OpenTime,
        decimal Open,
        decimal High,
        decimal Low,
        decimal Close,
        decimal Volume,
        DateTime CloseTime,
        decimal QuoteAssetVolume,
        int NumberOfTrades,
        decimal TakerBuyBaseAssetVolume,
        decimal TakerBuyQuoteAssetVolume,
        decimal Ignore);
}