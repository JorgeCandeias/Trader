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
        // populate from the default symbol
        if (!IsNullOrEmpty(context.Symbol.Name))
        {
            context.Savings = await GetPositionsAsync(context.Symbol, cancellationToken).ConfigureAwait(false);

            context.SavingsLookup[context.Symbol.Name] = context.Savings;
        }

        // populate from the symbol list
        foreach (var symbol in context.Symbols)
        {
            if (symbol.Key == context.Symbol.Name)
            {
                continue;
            }

            context.SavingsLookup[symbol.Key] = await GetPositionsAsync(symbol.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<SymbolSavingsPositions> GetPositionsAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        var baseAsset = await _savings
            .GetPositionOrZeroAsync(symbol.BaseAsset, cancellationToken)
            .ConfigureAwait(false);

        var quoteAsset = await _savings
            .GetPositionOrZeroAsync(symbol.QuoteAsset, cancellationToken)
            .ConfigureAwait(false);

        return new SymbolSavingsPositions(symbol.Name, baseAsset, quoteAsset);
    }
}