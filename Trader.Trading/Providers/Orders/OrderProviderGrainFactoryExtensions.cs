using Outcompute.Trader.Trading.Providers.Balances;
using Outcompute.Trader.Trading.Providers.Orders;
using Outcompute.Trader.Trading.Providers.Tickers;
using Outcompute.Trader.Trading.Providers.Trades;

namespace Orleans
{
    internal static class OrderProviderGrainFactoryExtensions
    {
        public static IOrderProviderGrain GetOrderProviderGrain(this IGrainFactory factory, string symbol)
        {
            return factory.GetGrain<IOrderProviderGrain>(symbol);
        }
    }
}