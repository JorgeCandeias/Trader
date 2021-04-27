using Trader.Models.Collections;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ImmutableSortedOrderSetServiceCollectionExtensions
    {
        public static IServiceCollection AddModelConverters(this IServiceCollection services)
        {
            return services
                .AddSingleton(typeof(ImmutableSortedOrderSetConverter<>))
                .AddAutoMapper(options =>
                {
                    options.AddProfile<ImmutableSortedOrderSetProfile>();
                });
        }
    }
}