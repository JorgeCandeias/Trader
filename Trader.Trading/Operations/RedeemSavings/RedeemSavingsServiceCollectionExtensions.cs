using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Operations.RedeemSavings
{
    internal static class RedeemSavingsServiceCollectionExtensions
    {
        public static IServiceCollection AddRedeemSavingsServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IRedeemSavingsOperation, RedeemSavingsOperation>()
                .AddSingleton<IAlgoResultExecutor<RedeemSavingsAlgoResult>, RedeemSavingsExecutor>();
        }
    }
}