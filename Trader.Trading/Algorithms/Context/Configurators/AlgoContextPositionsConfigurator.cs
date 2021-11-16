using Microsoft.Extensions.Options;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextPositionsConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly IOptionsMonitor<AlgoOptions> _options;
    private readonly IAutoPositionResolver _resolver;

    public AlgoContextPositionsConfigurator(IOptionsMonitor<AlgoOptions> monitor, IAutoPositionResolver resolver)
    {
        _options = monitor;
        _resolver = resolver;
    }

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        var options = _options.Get(name);

        // populate from the default symbol
        if (!IsNullOrEmpty(context.Symbol.Name))
        {
            context.PositionDetailsLookup[context.Symbol.Name] = await _resolver
                .ResolveAsync(context.Symbol, options.StartTime, cancellationToken)
                .ConfigureAwait(false);
        }

        // populate from the symbol list
        if (context.Symbols.Count > 0)
        {
            foreach (var symbol in context.Symbols)
            {
                if (symbol.Key == context.Symbol.Name)
                {
                    continue;
                }

                context.PositionDetailsLookup[symbol.Key] = await _resolver
                    .ResolveAsync(symbol.Value, options.StartTime, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}