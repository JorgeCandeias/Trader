using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

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

        // get klines for the symbol list
        if (context.Symbols.Count > 0 && context.KlineInterval != KlineInterval.None && context.KlinePeriods > 0)
        {
            foreach (var symbol in context.Symbols)
            {
                await ApplyAsync(context, symbol, cancellationToken).ConfigureAwait(false);
            }
        }

        // get klines for the default symbol
        if (!IsNullOrEmpty(context.Symbol.Name) && context.KlineInterval != KlineInterval.None && context.KlinePeriods > 0)
        {
            await ApplyAsync(context, context.Symbol, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ApplyAsync(AlgoContext context, Symbol symbol, CancellationToken cancellationToken)
    {
        context.Data.GetOrAdd(symbol.Name).Klines = await _klines
            .GetKlinesAsync(symbol.Name, context.KlineInterval, context.TickTime, context.KlinePeriods, cancellationToken)
            .ConfigureAwait(false);
    }
}