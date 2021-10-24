using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Operations;
using Outcompute.Trader.Trading.Operations.AveragingSell;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class AveragingSellServiceCollectionExtensions
    {
        public static IServiceCollection AddAveragingSellServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IAveragingSellOperation, AveragingSellOperation>()
                .AddSingleton<IAlgoResultExecutor<AveragingSellResult>, AveragingSellExecutor>();
        }
    }
}