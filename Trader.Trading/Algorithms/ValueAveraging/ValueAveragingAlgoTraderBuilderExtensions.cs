using Outcompute.Trader.Trading.Algorithms.ValueAveraging;

namespace Outcompute.Trader.Hosting
{
    public static class ValueAveragingAlgoTraderBuilderExtensions
    {
        public static ITraderBuilder AddValueAveragingAlgo(this ITraderBuilder trader)
        {
            return trader
                .AddAlgoType<ValueAveragingAlgo, ValueAveragingAlgoOptions>("ValueAveraging");
        }
    }
}