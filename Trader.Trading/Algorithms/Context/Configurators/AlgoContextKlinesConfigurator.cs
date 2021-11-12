using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    internal class AlgoContextKlinesConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        private readonly IOptionsMonitor<AlgoOptions> _options;
        private readonly IKlineProvider _klines;

        public AlgoContextKlinesConfigurator(IOptionsMonitor<AlgoOptions> options, IKlineProvider klines)
        {
            _options = options;
            _klines = klines;
        }

        public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
        {
            var options = _options.Get(name);

            foreach (var dependency in options.DependsOn.Klines)
            {
                context.Klines[(dependency.Symbol, dependency.Interval)] = await _klines
                    .GetKlinesAsync(dependency.Symbol, dependency.Interval, context.TickTime, dependency.Periods, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}