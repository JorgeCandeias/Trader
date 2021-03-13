using Trader.Core.Trading.ProfitCalculation;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ProfitCalculatorServiceCollectionExtensions
    {
        public static IServiceCollection AddProfitCalculator(this IServiceCollection services)
        {
            return services
                .AddSingleton<IProfitCalculator, ProfitCalculator>();
        }
    }
}