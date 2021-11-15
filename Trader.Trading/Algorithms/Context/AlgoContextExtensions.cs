namespace Outcompute.Trader.Trading.Algorithms.Context
{
    public static class AlgoContextExtensions
    {
        public static decimal GetFreeBaseAssetBalance(this IAlgoContext context, bool withSavings = false, bool withSwapPool = false)
        {
            return context.BaseAssetSpotBalance.Free
                + (withSavings ? context.BaseAssetSavingsBalance.FreeAmount : 0)
                + (withSwapPool ? context.BaseAssetSwapPoolBalance.Total : 0);
        }

        public static decimal GetQuoteBaseAssetBalance(this IAlgoContext context, bool withSavings = false, bool withSwapPool = false)
        {
            return context.QuoteAssetSpotBalance.Free
                + (withSavings ? context.QuoteAssetSavingsBalance.FreeAmount : 0)
                + (withSwapPool ? context.QuoteAssetSwapPoolBalance.Total : 0);
        }
    }
}