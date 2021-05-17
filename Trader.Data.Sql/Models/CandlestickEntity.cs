using System;

namespace Trader.Data.Sql.Models
{
    internal record CandlestickEntity(
        int SymbolId,
        int Interval,
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