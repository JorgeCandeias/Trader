using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Hosting
{
    public static class AlgoFactoryTraderBuilderExtensions
    {
        public static ITraderBuilder AddAlgoType<TAlgo>(this ITraderBuilder builder, string typeName)
            where TAlgo : IAlgo
        {
            return builder
                .ConfigureServices(services =>
                {
                    services.AddNamedSingleton<IAlgoFactory, AlgoFactory<TAlgo>>(typeName);
                });
        }

        public static ITraderBuilder AddAlgoType<TAlgo, TOptions>(this ITraderBuilder builder, string typeName)
            where TAlgo : IAlgo
            where TOptions : class
        {
            return builder
                .AddAlgoType<TAlgo>(typeName)
                .ConfigureServices(services =>
                {
                    services.ConfigureOptions<AlgoOptionsConfigurator<TOptions>>();
                });
        }
    }
}