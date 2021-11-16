using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextTradesConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly ITradeProvider _trades;

    public AlgoContextTradesConfigurator(ITradeProvider trades)
    {
        _trades = trades;
    }

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        if (IsNullOrWhiteSpace(context.Symbol.Name))
        {
            return;
        }

        context.Trades = await _trades
            .GetTradesAsync(context.Symbol.Name, cancellationToken)
            .ConfigureAwait(false);
    }
}