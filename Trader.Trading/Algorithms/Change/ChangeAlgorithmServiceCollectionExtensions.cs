using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Change;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ChangeAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddChangeAlgorithm(this IServiceCollection services, string name, Action<ChangeAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ISymbolAlgo, ChangeAlgorithm>(sp => ActivatorUtilities.CreateInstance<ChangeAlgorithm>(sp, name))
                .AddOptions<ChangeAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}