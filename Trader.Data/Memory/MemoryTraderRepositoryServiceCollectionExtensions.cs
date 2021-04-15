using Microsoft.Extensions.Hosting;
using Trader.Data;
using Trader.Data.Memory;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MemoryTraderRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddMemoryRepository(this IServiceCollection services)
        {
            return services
                .AddSingleton<MemoryTraderRepository>()
                .AddSingleton<ITraderRepository>(sp => sp.GetRequiredService<MemoryTraderRepository>())
                .AddSingleton<IHostedService>(sp => sp.GetRequiredService<MemoryTraderRepository>());
        }
    }
}