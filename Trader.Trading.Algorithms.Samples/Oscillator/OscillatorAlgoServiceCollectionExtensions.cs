using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Samples.Oscillator;

namespace Microsoft.Extensions.DependencyInjection;

public static class OscillatorAlgoServiceCollectionExtensions
{
    internal const string AlgoTypeName = "Oscillator";

    public static IServiceCollection AddOscillatorAlgoType(this IServiceCollection services)
    {
        return services
            .AddAlgoType<OscillatorAlgo>(AlgoTypeName)
            .AddOptionsType<OscillatorAlgoOptions>()
            .Services;
    }

    public static IAlgoBuilder<IAlgo, OscillatorAlgoOptions> AddOscillatorAlgo(this IServiceCollection services, string name)
    {
        return services.AddAlgo<IAlgo, OscillatorAlgoOptions>(name, AlgoTypeName);
    }
}