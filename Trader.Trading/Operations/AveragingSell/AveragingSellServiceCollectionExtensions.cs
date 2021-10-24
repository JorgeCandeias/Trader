using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Operations.AveragingSell
{
    internal static class AveragingSellServiceCollectionExtensions
    {
        public static IServiceCollection AddAveragingSellServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IAveragingSellOperation, AveragingSellOperation>()
                .AddSingleton<IAlgoResultExecutor<AveragingSellAlgoResult>, AveragingSellExecutor>();
        }
    }
}