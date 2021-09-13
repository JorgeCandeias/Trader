using Outcompute.Trader.Trading.Algorithms.Step;

namespace Outcompute.Trader.Hosting
{
    public static class StepAlgoTraderBuilderExtensions
    {
        public static ITraderBuilder AddStepAlgo(this ITraderBuilder trader)
        {
            return trader
                .AddAlgoType<StepAlgo, StepAlgoOptions>("Step");
        }
    }
}