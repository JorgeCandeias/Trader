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
            await ApplyAsync(context, symbol, cancellationToken).ConfigureAwait(false);
        }

        if (!IsNullOrEmpty(context.Symbol.Name) && !context.Symbols.Contains(context.Symbol.Name))
        {
            await ApplyAsync(context, context.Symbol, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ApplyAsync(AlgoContext context, Symbol symbol, CancellationToken cancellationToken)
    {
        var savings = context.Data.GetOrAdd(symbol.Name).Savings;

        savings.BaseAsset = await _savings
            .GetBalanceOrZeroAsync(symbol.BaseAsset, cancellationToken)
            .ConfigureAwait(false);

        savings.QuoteAsset = await _savings
            .GetBalanceOrZeroAsync(symbol.QuoteAsset, cancellationToken)
            .ConfigureAwait(false);
    }
}