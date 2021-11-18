using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextTickerConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly ITickerProvider _tickers;

    public AlgoContextTickerConfigurator(ITickerProvider tickers)
    {
        _tickers = tickers;
    }

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        foreach (var symbol in context.Symbols)
        {
            await ApplyAsync(context, symbol, cancellationToken).ConfigureAwait(false);
        }

        if (!IsNullOrEmpty(context.Symbol.Name) && !context.Symbols.Contains(context.Symbol.Name))
        {
            await ApplyAsync(context, context.Symbol, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ApplyAsync(AlgoContext context, Symbol symbol, CancellationToken cancellationToken)
    {
        context.Data.GetOrAdd(symbol.Name).Ticker = await _tickers
            .GetRequiredTickerAsync(symbol.Name, cancellationToken)
            .ConfigureAwait(false);
    }
}