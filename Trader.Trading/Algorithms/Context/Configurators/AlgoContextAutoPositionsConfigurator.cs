using Microsoft.Extensions.Options;
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

    public ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        var options = _options.Get(name);

        foreach (var symbol in context.Symbols)
        {
            context.Data.GetOrAdd(symbol.Name).AutoPosition = _resolver.Resolve(symbol, context.Orders.Filled, context.Trades, options.StartTime);
        }

        return ValueTask.CompletedTask;
    }
}