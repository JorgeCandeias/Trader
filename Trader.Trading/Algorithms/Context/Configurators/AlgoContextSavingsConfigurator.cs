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
            var savings = context.Data[symbol.Name].Savings;

            savings.BaseAsset = await _savings
                .GetBalanceOrZeroAsync(symbol.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            savings.QuoteAsset = await _savings
                .GetBalanceOrZeroAsync(symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}