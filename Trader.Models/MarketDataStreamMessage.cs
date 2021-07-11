namespace Trader.Models
{
    public record MarketDataStreamMessage(
        ExternalError? Error,
        MiniTicker? MiniTicker,
        Kline? Kline)
    {
        public static MarketDataStreamMessage Empty { get; } = new MarketDataStreamMessage(null, null, null);
    }
}