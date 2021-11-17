using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextSwapPoolBalanceConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly ISwapPoolProvider _swaps;

    public AlgoContextSwapPoolBalanceConfigurator(ISwapPoolProvider swaps)
    {
        _swaps = swaps;
    }

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        foreach (var symbol in context.Symbols)
        {
            if (!context.SwapPoolBalances.TryGetValue(symbol.Key, out var balances))
            {
                context.SwapPoolBalances[symbol.Key] = balances = new SymbolSwapPoolAssetBalances
                {
                    Symbol = symbol.Value
                };
            }

            balances.BaseAsset = await _swaps
                .GetBalanceAsync(symbol.Value.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            balances.QuoteAsset = await _swaps
                .GetBalanceAsync(symbol.Value.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}