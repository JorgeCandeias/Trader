using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Blocks;
using Outcompute.Trader.Trading.Blocks.AveragingSell;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class AveragingSellServiceCollectionExtensions
    {
        public static IServiceCollection AddAveragingSellServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IAveragingSellBlock, AveragingSellBlock>()
                .AddSingleton<IAlgoResultExecutor<AveragingSellResult>, AveragingSellExecutor>();
        }
    }
}