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
            var swaps = context.Data.GetOrAdd(symbol.Key).SwapPools;

            swaps.BaseAsset = await _swaps
                .GetBalanceAsync(symbol.Value.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            swaps.QuoteAsset = await _swaps
                .GetBalanceAsync(symbol.Value.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}