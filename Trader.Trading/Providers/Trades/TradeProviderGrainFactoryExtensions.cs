using Outcompute.Trader.Trading.Providers.Trades;

namespace Orleans;

internal static class TradeProviderGrainFactoryExtensions
{
    public static ITradeProviderGrain GetTradeProviderGrain(this IGrainFactory factory, string symbol)
    {
        return factory.GetGrain<ITradeProviderGrain>(symbol);
    }
}