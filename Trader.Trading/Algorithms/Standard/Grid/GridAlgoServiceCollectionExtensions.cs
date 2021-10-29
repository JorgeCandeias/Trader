using Outcompute.Trader.Trading.Algorithms.Standard.Grid;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GridAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddStepAlgo(this IServiceCollection trader)
        {
            return trader
                .AddAlgoType<GridAlgo, GridAlgoOptions>("Grid");
        }
    }
}