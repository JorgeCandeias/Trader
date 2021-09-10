using Outcompute.Trader.Core.Randomizers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RandomGeneratorServiceCollectionExtensions
    {
        public static IServiceCollection AddRandomGenerator(this IServiceCollection services)
        {
            return services.AddSingleton<IRandomGenerator, RandomGenerator>();
        }
    }
}