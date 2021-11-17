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

            var index = context.Symbols.IndexOf(value);
            if (index < 0)
            {
                context.Symbols.Add(value);
            }
            else
            {
                context.Symbols[index] = value;
            }
        }

        // populate the default symbol
        if (!IsNullOrEmpty(options.Symbol))
        {
            context.Symbol = context.Symbols[options.Symbol];
        }

        return ValueTask.CompletedTask;
    }
}