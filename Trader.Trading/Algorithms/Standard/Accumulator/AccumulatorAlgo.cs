using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Accumulator
{
    internal class AccumulatorAlgo : SymbolAlgo
    {
        private readonly AccumulatorAlgoOptions _options;

        public AccumulatorAlgo(IOptionsSnapshot<AccumulatorAlgoOptions> options)
        {
            _options = options.Get(Context.Name);
        }

        public override Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            return TrackingBuy(Context.Symbol, _options.PullbackRatio, _options.TargetQuoteBalanceFractionPerBuy, _options.MaxNotional, _options.RedeemSavings)
                .AsTaskResult<IAlgoCommand>();
        }
    }
}