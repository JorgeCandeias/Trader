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

        // populate the default symbol
        if (!IsNullOrEmpty(options.Symbol))
        {
            context.Symbol = _exchange.GetRequiredSymbol(options.Symbol);
        }

        // populate the symbol set
        foreach (var symbol in options.Symbols)
        {
            context.Symbols[symbol] = _exchange.GetRequiredSymbol(symbol);
        }

        return ValueTask.CompletedTask;
    }
}