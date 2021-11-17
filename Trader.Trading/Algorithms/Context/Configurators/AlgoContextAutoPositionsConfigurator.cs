using Microsoft.Extensions.Options;

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
            context.Data.GetOrAdd(symbol.Key).AutoPosition = await _resolver
                .ResolveAsync(symbol.Value, options.StartTime, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}