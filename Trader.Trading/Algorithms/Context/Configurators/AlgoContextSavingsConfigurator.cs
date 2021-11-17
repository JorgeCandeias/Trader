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
            var savings = context.Data.GetOrAdd(symbol.Key).Savings;

            savings.BaseAsset = await _savings
                .GetBalanceOrZeroAsync(symbol.Value.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            savings.QuoteAsset = await _savings
                .GetBalanceOrZeroAsync(symbol.Value.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}