using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextTradesConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly ITradeProvider _trades;

    public AlgoContextTradesConfigurator(ITradeProvider trades)
    {
        _trades = trades;
    }

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        foreach (var symbol in context.Symbols)
        {
            await ApplyAsync(context, symbol, cancellationToken).ConfigureAwait(false);
        }

        if (!IsNullOrEmpty(context.Symbol.Name) && !context.Symbols.Contains(context.Symbol.Name))
        {
            await ApplyAsync(context, context.Symbol, cancellationToken);
        }
    }

    private async ValueTask ApplyAsync(AlgoContext context, Symbol symbol, CancellationToken cancellationToken)
    {
        context.Data.GetOrAdd(symbol.Name).Trades = await _trades
            .GetTradesAsync(symbol.Name, cancellationToken)
            .ConfigureAwait(false);
    }
}