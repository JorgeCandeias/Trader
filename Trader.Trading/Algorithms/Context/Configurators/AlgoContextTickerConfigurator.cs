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
        foreach (var symbol in context.Symbols.Keys)
        {
            context.Data.GetOrAdd(symbol).Ticker = await _tickers
                .GetRequiredTickerAsync(symbol, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}