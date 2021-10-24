using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Operations.SignificantAveragingSell
{
    internal static class SignificantAveragingSellServiceCollectionExtensions
    {
        public static IServiceCollection AddSignificantAveragingSellServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<ISignificantAveragingSellOperation, SignificantAveragingSellOperation>()
                .AddSingleton<IAlgoResultExecutor<SignificantAveragingSellAlgoResult>, SignificantAveragingSellExecutor>();
        }
    }
}