using Outcompute.Trader.Trading.Algorithms.Positions;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    /// <summary>
    /// This configurator resets all the context properties to a clean slate before other configurators execute.
    /// </summary>
    internal class AlgoContextResetConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        public ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
        {
            foreach (var item in context.Data)
            {
                item.Exceptions.Clear();
                item.Symbol = Symbol.Empty;
                item.AutoPosition = AutoPosition.Empty;
                item.Ticker = MiniTicker.Empty;
                item.Spot.BaseAsset = Balance.Empty;
                item.Spot.QuoteAsset = Balance.Empty;
                item.Savings.BaseAsset = SavingsBalance.Empty;
                item.Savings.QuoteAsset = SavingsBalance.Empty;
                item.SwapPools.BaseAsset = SwapPoolAssetBalance.Empty;
                item.SwapPools.QuoteAsset = SwapPoolAssetBalance.Empty;
                item.Orders.Completed = ImmutableSortedSet<OrderQueryResult>.Empty;
                item.Orders.Open = ImmutableSortedSet<OrderQueryResult>.Empty;
                item.Orders.Filled = ImmutableSortedSet<OrderQueryResult>.Empty;
                item.Trades = TradeCollection.Empty;
                item.Klines = KlineCollection.Empty;
            }

            return ValueTask.CompletedTask;
        }
    }
}