using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;

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

            // populate the default settings
            context.KlineInterval = options.KlineInterval;
            context.KlinePeriods = options.KlinePeriods;

            // get klines for the default settings
            if (!IsNullOrEmpty(context.Symbol.Name) && context.KlineInterval != KlineInterval.None && context.KlinePeriods > 0)
            {
                context.KlineLookup[(context.Symbol.Name, context.KlineInterval)] = context.Klines = await _klines
                    .GetKlinesAsync(context.Symbol.Name, context.KlineInterval, context.TickTime, context.KlinePeriods, cancellationToken)
                    .ConfigureAwait(false);
            }

            // get klines for extra dependencies
            foreach (var dependency in options.DependsOn.Klines)
            {
                var symbol = dependency.Symbol ?? context.Symbol.Name;
                var interval = dependency.Interval is not KlineInterval.None ? dependency.Interval : context.KlineInterval;
                var periods = dependency.Periods is not 0 ? dependency.Periods : context.KlinePeriods;

                if (!IsNullOrEmpty(symbol) && interval != KlineInterval.None && periods > 0)
                {
                    context.KlineLookup[(symbol, interval)] = await _klines
                        .GetKlinesAsync(symbol, interval, context.TickTime, periods, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}