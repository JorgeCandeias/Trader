using Outcompute.Trader.Trading.Providers.Tickers;

namespace Orleans
{
    internal static class TickerProviderGrainFactoryExtensions
    {
        public static ITickerProviderGrain GetTickerProviderGrain(this IGrainFactory factory, string symbol)
        {
            return factory.GetGrain<ITickerProviderGrain>(symbol);
        }
    }
}