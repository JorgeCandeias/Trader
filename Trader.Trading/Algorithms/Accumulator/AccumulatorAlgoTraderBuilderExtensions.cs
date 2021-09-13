using Outcompute.Trader.Trading.Algorithms.Accumulator;

namespace Outcompute.Trader.Hosting
{
    public static class AccumulatorAlgoTraderBuilderExtensions
    {
        public static ITraderBuilder AddAccumulatorAlgo(this ITraderBuilder trader)
        {
            return trader
                .AddAlgoType<AccumulatorAlgo, AccumulatorAlgoOptions>("Accumulator");
        }
    }
}