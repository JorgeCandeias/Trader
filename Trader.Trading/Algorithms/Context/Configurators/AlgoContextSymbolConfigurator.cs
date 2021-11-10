using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    internal class AlgoContextSymbolConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        private readonly IExchangeInfoProvider _exchange;
        private readonly IOptionsMonitor<AlgoOptions> _monitor;

        public AlgoContextSymbolConfigurator(IExchangeInfoProvider exchange, IOptionsMonitor<AlgoOptions> monitor)
        {
            _exchange = exchange;
            _monitor = monitor;
        }

        public async ValueTask ConfigureAsync(AlgoContext context, string symbol, DateTime startTime, CancellationToken cancellationToken = default)
        {
            context.Symbol = await _exchange
                .GetRequiredSymbolAsync(symbol, cancellationToken)
                .ConfigureAwait(false);
        }

        public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
        {
            var options = _monitor.Get(name);

            if (options.Symbol.IsNullOrWhiteSpace())
            {
                return;
            }

            context.Symbol = await _exchange
                .GetRequiredSymbolAsync(options.Symbol, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}