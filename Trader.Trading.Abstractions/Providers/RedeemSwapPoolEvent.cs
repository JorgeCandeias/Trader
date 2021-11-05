using Orleans.Concurrency;

namespace Outcompute.Trader.Trading.Providers
{
    [Immutable]
    public record RedeemSwapPoolEvent(bool Success, string QuoteAsset, decimal QuoteAmount, string BaseAsset, decimal BaseAmount);
}