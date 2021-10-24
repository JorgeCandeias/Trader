using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Operations.CreateOrder
{
    internal static class CreateOrderServiceCollectionExtensions
    {
        public static IServiceCollection AddCreateOrderServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<ICreateOrderOperation, CreateOrderOperation>()
                .AddSingleton<IAlgoResultExecutor<CreateOrderAlgoResult>, CreateOrderExecutor>();
        }
    }
}