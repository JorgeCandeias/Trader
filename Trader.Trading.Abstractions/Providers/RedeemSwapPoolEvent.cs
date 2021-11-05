using Orleans.Concurrency;

namespace Outcompute.Trader.Trading.Providers
{
    [Immutable]
    public record RedeemSwapPoolEvent(bool Success, string QuoteAsset, decimal QuoteAmount, string BaseAsset, decimal BaseAmount)
    {
        public static RedeemSwapPoolEvent Failed(string quoteAsset) => new(false, quoteAsset, 0, string.Empty, 0);
    }
}