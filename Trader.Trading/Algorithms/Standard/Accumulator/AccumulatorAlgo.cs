using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Commands;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Accumulator
{
    internal class AccumulatorAlgo : Algo
    {
        private readonly IOptionsMonitor<AccumulatorAlgoOptions> _options;

        public AccumulatorAlgo(IOptionsMonitor<AccumulatorAlgoOptions> options)
        {
            _options = options;
        }

        protected override async ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            // fix options for this run
            var options = _options.Get(Context.Name);

            // calculate the current rsi
            var rsi = Context.Klines[(Context.Symbol.Name, options.RsiInterval)].LastRsi(x => x.ClosePrice, options.RsiPeriods);

            return Noop();
        }
    }
}