using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Accumulator
{
    internal class AccumulatorAlgo : SymbolAlgo
    {
        private readonly IOptionsMonitor<AccumulatorAlgoOptions> _options;

        public AccumulatorAlgo(IOptionsMonitor<AccumulatorAlgoOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public override Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            var options = _options.Get(Context.Name);

            return TrackingBuy(Context.Symbol, options.PullbackRatio, options.TargetQuoteBalanceFractionPerBuy, options.MaxNotional, options.RedeemSavings)
                .AsTaskResult<IAlgoCommand>();
        }
    }
}