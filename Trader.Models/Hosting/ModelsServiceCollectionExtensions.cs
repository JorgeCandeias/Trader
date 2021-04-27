using Trader.Models.Collections;

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
                .AddAutoMapper(options =>
                {
                    options.AddProfile<ImmutableSortedOrderSetProfile>();
                    options.AddProfile<ImmutableSortedTradeSetProfile>();
                });
        }
    }
}