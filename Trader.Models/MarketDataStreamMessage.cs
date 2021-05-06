namespace Trader.Models
{
    public record MarketDataStreamMessage(
        ExternalError? Error,
        MiniTicker? MiniTicker);
}