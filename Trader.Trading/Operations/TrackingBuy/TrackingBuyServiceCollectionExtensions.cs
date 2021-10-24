using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Operations.TrackingBuy
{
    internal static class TrackingBuyServiceCollectionExtensions
    {
        public static IServiceCollection AddTrackingBuyServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<ITrackingBuyOperation, TrackingBuyOperation>()
                .AddSingleton<IAlgoResultExecutor<TrackingBuyAlgoResult>, TrackingBuyExecutor>();
        }
    }
}