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

        // populate from the symbol list
        if (context.Symbols.Count > 0)
        {
            foreach (var symbol in context.Symbols)
            {
                context.PositionDetailsLookup[symbol.Key] = await _resolver
                    .ResolveAsync(symbol.Value, options.StartTime, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}