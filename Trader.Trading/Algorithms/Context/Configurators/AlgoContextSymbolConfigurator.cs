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

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        var options = _monitor.Get(name);

        // populate the default symbol
        if (IsNullOrEmpty(options.Symbol))
        {
            context.Symbol = await _exchange
                .GetRequiredSymbolAsync(options.Symbol, cancellationToken)
                .ConfigureAwait(false);
        }

        // populate the symbol set
        foreach (var symbol in options.Symbols)
        {
            context.Symbols[symbol] = await _exchange
                .GetRequiredSymbolAsync(symbol, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}