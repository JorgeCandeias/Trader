using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextSymbolConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly IOptionsMonitor<AlgoOptions> _monitor;
    private readonly IExchangeInfoProvider _exchange;

    public AlgoContextSymbolConfigurator(IOptionsMonitor<AlgoOptions> monitor, IExchangeInfoProvider exchange)
    {
        _monitor = monitor;
        _exchange = exchange;
    }

    public ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        var options = _monitor.Get(name);

        // populate the symbol set
        foreach (var symbol in options.Symbols)
        {
            var value = _exchange.GetRequiredSymbol(symbol);

            context.Symbols.AddOrUpdate(value);
            context.Data.GetOrAdd(symbol).Symbol = value;
        }

        // populate the default symbol
        if (!IsNullOrEmpty(options.Symbol))
        {
            context.Symbol = context.Data[options.Symbol].Symbol;
        }

        return ValueTask.CompletedTask;
    }
}