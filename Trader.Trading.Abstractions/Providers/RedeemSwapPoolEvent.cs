using Orleans.Concurrency;

namespace Outcompute.Trader.Trading.Providers
{
    [Immutable]
    public record RedeemSwapPoolEvent(bool Success, string PoolName, string QuoteAsset, decimal QuoteAmount, string BaseAsset, decimal BaseAmount)
    {
        public static RedeemSwapPoolEvent Failed(string quoteAsset) => new(false, string.Empty, quoteAsset, 0, string.Empty, 0);
    }
}