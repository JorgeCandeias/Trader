using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Samples.Pusher;

namespace Microsoft.Extensions.DependencyInjection;

public static class PusherAlgoServiceCollectionExtensions
{
    internal const string AlgoTypeName = "Pusher";

    public static IServiceCollection AddPusherAlgoType(this IServiceCollection services)
    {
        return services
            .AddAlgoType<PusherAlgo>(AlgoTypeName)
            .AddOptionsType<PusherAlgoOptions>()
            .Services;
    }

    public static IAlgoBuilder<IAlgo, PusherAlgoOptions> AddOscillatorAlgo(this IServiceCollection services, string name)
    {
        return services.AddAlgo<IAlgo, PusherAlgoOptions>(name, AlgoTypeName);
    }
}