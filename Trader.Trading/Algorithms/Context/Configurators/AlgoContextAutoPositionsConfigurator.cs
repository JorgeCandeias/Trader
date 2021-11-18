using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Positions;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextAutoPositionsConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly IOptionsMonitor<AlgoOptions> _options;
    private readonly IAutoPositionResolver _resolver;

    public AlgoContextAutoPositionsConfigurator(IOptionsMonitor<AlgoOptions> monitor, IAutoPositionResolver resolver)
    {
        _options = monitor;
        _resolver = resolver;
    }

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        var options = _options.Get(name);

        foreach (var symbol in context.Symbols)
        {
            await ApplyAsync(context, symbol, options, cancellationToken).ConfigureAwait(false);
        }

        if (!IsNullOrEmpty(context.Symbol.Name) && !context.Symbols.Contains(context.Symbol.Name))
        {
            await ApplyAsync(context, context.Symbol, options, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ApplyAsync(AlgoContext context, Symbol symbol, AlgoOptions options, CancellationToken cancellationToken)
    {
        context.Data.GetOrAdd(symbol.Name).AutoPosition = await _resolver
            .ResolveAsync(symbol, options.StartTime, cancellationToken)
            .ConfigureAwait(false);
    }
}