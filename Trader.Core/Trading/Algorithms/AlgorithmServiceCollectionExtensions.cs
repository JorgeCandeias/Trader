using Trader.Core.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgorithmResolvers(this IServiceCollection services)
        {
            return services
                .AddSingleton<ISignificantOrderResolver, SignificantOrderResolver>();
        }
    }
}