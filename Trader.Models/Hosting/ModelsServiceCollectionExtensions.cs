using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Models.Hosting;
using System.Collections.Immutable;

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
                .AddSingleton(typeof(ImmutableSortedOrderSetConverter<>))
                .AddSingleton(typeof(ImmutableSortedTradeSetConverter<>))
                .AddSingleton(typeof(ImmutableListConverter<,>))
                .AddSingleton(typeof(ImmutableDictionaryConverter<,,,>))
                .AddSingleton(typeof(ImmutableHashSetConverter<,>))
                .AddAutoMapper(options =>
                {
                    options.AddProfile<ImmutableSortedOrderSetProfile>();
                    options.AddProfile<ImmutableSortedTradeSetProfile>();
                    options.AddProfile<ModelsProfile>();

                    options.CreateMap(typeof(IEnumerable<>), typeof(ImmutableList<>)).ConvertUsing(typeof(ImmutableListConverter<,>));
                    options.CreateMap(typeof(IDictionary<,>), typeof(ImmutableDictionary<,>)).ConvertUsing(typeof(ImmutableDictionaryConverter<,,,>));
                    options.CreateMap(typeof(IEnumerable<>), typeof(ImmutableHashSet<>)).ConvertUsing(typeof(ImmutableHashSetConverter<,>));
                });
        }
    }
}