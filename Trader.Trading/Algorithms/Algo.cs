using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Blocks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// Base class for algos that do not follow the suggested lifecycle.
    /// For symbol based algos, consider implementing <see cref="SymbolAlgo"/> instead.
    /// </summary>
    public abstract class Algo : IAlgo
    {
        public abstract ValueTask GoAsync(CancellationToken cancellationToken = default);

        public virtual ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public IAlgoContext Context { get; set; } = NullAlgoContext.Instance;

        public virtual Task SetAveragingSellAsync(Symbol symbol, IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            return Context.ServiceProvider
                .GetRequiredService<IAveragingSellBlock>()
                .SetAveragingSellAsync(symbol, orders, profitMultiplier, redeemSavings, cancellationToken);
        }

        public virtual Task<OrderResult> CreateOrderAsync(Symbol symbol, OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag, CancellationToken cancellationToken = default)
        {
            return Context.ServiceProvider
                .GetRequiredService<ICreateOrderBlock>()
                .CreateOrderAsync(symbol, type, side, timeInForce, quantity, price, tag, cancellationToken);
        }

        public virtual Task<CancelStandardOrderResult> CancelOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            return Context.ServiceProvider
                .GetRequiredService<ICancelOrderBlock>()
                .CancelOrderAsync(symbol, orderId, cancellationToken);
        }

        public virtual Task<bool> EnsureSingleOrderAsync(Symbol symbol, OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            return Context.ServiceProvider
                .GetRequiredService<IEnsureSingleOrderBlock>()
                .EnsureSingleOrderAsync(symbol, side, type, timeInForce, quantity, price, redeemSavings, cancellationToken);
        }

        public virtual Task ClearOpenOrdersAsync(Symbol symbol, OrderSide side, CancellationToken cancellationToken = default)
        {
            return Context.ServiceProvider
                .GetRequiredService<IClearOpenOrdersBlock>()
                .ClearOpenOrdersAsync(symbol, side, cancellationToken);
        }

        public virtual Task<IReadOnlyList<OrderQueryResult>> GetOpenOrdersAsync(Symbol symbol, OrderSide side, CancellationToken cancellationToken = default)
        {
            return Context.ServiceProvider
                .GetRequiredService<IGetOpenOrdersBlock>()
                .GetOpenOrdersAsync(symbol, side, cancellationToken);
        }

        public virtual Task<(bool Success, decimal Redeemed)> TryRedeemSavingsAsync(string asset, decimal amount, CancellationToken cancellationToken = default)
        {
            return Context.ServiceProvider
                .GetRequiredService<IRedeemSavingsBlock>()
                .TryRedeemSavingsAsync(asset, amount, cancellationToken);
        }

        public virtual Task SetSignificantAveragingSellAsync(Symbol symbol, MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            return Context.ServiceProvider
                .GetRequiredService<ISignificantAveragingSellBlock>()
                .SetSignificantAveragingSellAsync(symbol, ticker, orders, minimumProfitRate, redeemSavings, cancellationToken);
        }

        public virtual Task<bool> SetTrackingBuyAsync(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            return Context.ServiceProvider
                .GetRequiredService<ITrackingBuyBlock>()
                .SetTrackingBuyAsync(symbol, pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, redeemSavings, cancellationToken);
        }
    }
}