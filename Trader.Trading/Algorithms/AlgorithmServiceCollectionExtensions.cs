﻿using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Steps;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddTraderAlgorithmBlocks(this IServiceCollection services)
        {
            return services
                .AddSingleton<ISignificantOrderResolver, SignificantOrderResolver>()
                .AddSingleton<IOrderSynchronizer, OrderSynchronizer>()
                .AddSingleton<ITradeSynchronizer, TradeSynchronizer>()
                .AddSingleton<IOrderCodeGenerator, OrderCodeGenerator>()
                .AddSingleton<ITrackingBuyStep, TrackingBuyStep>()
                .AddSingleton<IAveragingSellStep, AveragingSellStep>()
                .AddSingleton<IRedeemSavingsStep, RedeemSavingsStep>();
        }
    }
}