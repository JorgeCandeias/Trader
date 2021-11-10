using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    internal class AlgoContextSymbolConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        private readonly IOptionsMonitor<AlgoOptions> _monitor;
        private readonly IExchangeInfoProvider _exchange;

        public AlgoContextSymbolConfigurator(IOptionsMonitor<AlgoOptions> monitor, IExchangeInfoProvider exchange)
        {
            _monitor = monitor;
            _exchange = exchange;
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