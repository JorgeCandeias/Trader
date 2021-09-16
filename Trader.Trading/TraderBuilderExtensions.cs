using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Blocks;
using System;

namespace Outcompute.Trader.Hosting
{
    public static class TraderBuilderExtensions
    {
        public static ITraderBuilder AddTradingServices(this ITraderBuilder builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            return builder
                .ConfigureServices(services =>
                {
                    services
                        .AddSingleton<ITrackingBuyBlock, TrackingBuyBlock>()
                        .AddSingleton<IAveragingSellBlock, AveragingSellBlock>()
                        .AddSingleton<IRedeemSavingsBlock, RedeemSavingsBlock>();
                });
        }
    }
}