using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Models.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ModelsServiceCollectionExtensions
    {
        /// <summary>
        /// Adds shared business-to-business model type converter services for the specialized collections.
        /// </summary>
        public static IServiceCollection AddModelServices(this IServiceCollection services)
        {
            return services
                .AddSingleton(typeof(ImmutableListConverter<,>))
                .AddSingleton(typeof(ImmutableDictionaryConverter<,,,>))
                .AddSingleton(typeof(ImmutableHashSetConverter<,>))
                .AddSingleton(typeof(ImmutableSortedSetConverter<,>))
                .AddAutoMapper(options =>
                {
                    options.AddProfile<ModelsProfile>();
                });
        }
    }
}