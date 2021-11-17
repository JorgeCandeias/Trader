using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextSavingsConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly ISavingsProvider _savings;

    public AlgoContextSavingsConfigurator(ISavingsProvider savings)
    {
        _savings = savings;
    }

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        foreach (var symbol in context.Symbols)
        {
            context.Savings[symbol.Key] = await GetPositionsAsync(symbol.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<SymbolSavingsBalances> GetPositionsAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        var baseAsset = await _savings
            .GetBalanceOrZeroAsync(symbol.BaseAsset, cancellationToken)
            .ConfigureAwait(false);

        var quoteAsset = await _savings
            .GetBalanceOrZeroAsync(symbol.QuoteAsset, cancellationToken)
            .ConfigureAwait(false);

        return new SymbolSavingsBalances(symbol.Name, baseAsset, quoteAsset);
    }
}