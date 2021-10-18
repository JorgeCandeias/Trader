using Outcompute.Trader.Trading.Providers;

namespace Orleans
{
    internal static class IOrderProviderReplicaGrainExtensions
    {
        public static IOrderProviderReplicaGrain GetOrderProviderReplicaGrain(this IGrainFactory factory, string symbol)
        {
            return factory.GetGrain<IOrderProviderReplicaGrain>(symbol);
        }
    }
}