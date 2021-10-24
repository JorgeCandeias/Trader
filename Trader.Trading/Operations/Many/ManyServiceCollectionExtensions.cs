using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Operations.Many
{
    internal static class ManyServiceCollectionExtensions
    {
        public static IServiceCollection AddManyServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IAlgoResultExecutor<ManyAlgoResult>, ManyExecutor>();
        }
    }
}