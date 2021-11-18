using Outcompute.Trader.Models;
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
            await ApplyAsync(context, symbol, cancellationToken).ConfigureAwait(false);
        }

        if (!IsNullOrEmpty(context.Symbol.Name) && !context.Symbols.Contains(context.Symbol.Name))
        {
            await ApplyAsync(context, context.Symbol, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ApplyAsync(AlgoContext context, Symbol symbol, CancellationToken cancellationToken)
    {
        var swaps = context.Data.GetOrAdd(symbol.Name).SwapPools;

        swaps.BaseAsset = await _swaps
            .GetBalanceAsync(symbol.BaseAsset, cancellationToken)
            .ConfigureAwait(false);

        swaps.QuoteAsset = await _swaps
            .GetBalanceAsync(symbol.QuoteAsset, cancellationToken)
            .ConfigureAwait(false);
    }
}