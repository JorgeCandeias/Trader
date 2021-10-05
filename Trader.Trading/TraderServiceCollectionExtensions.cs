using Outcompute.Trader.Trading.Blocks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TraderServiceCollectionExtensions
    {
        public static IServiceCollection AddTradingServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<ITrackingBuyBlock, TrackingBuyBlock>()
                .AddSingleton<IAveragingSellBlock, AveragingSellBlock>()
                .AddSingleton<IRedeemSavingsBlock, RedeemSavingsBlock>();
        }
    }
}