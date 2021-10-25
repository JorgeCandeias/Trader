using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Operations.AveragingSell;
using Outcompute.Trader.Trading.Operations.CancelOrder;
using Outcompute.Trader.Trading.Operations.ClearOpenOrders;
using Outcompute.Trader.Trading.Operations.CreateOrder;
using Outcompute.Trader.Trading.Operations.EnsureSingleOrder;
using Outcompute.Trader.Trading.Operations.Many;
using Outcompute.Trader.Trading.Operations.RedeemSavings;
using Outcompute.Trader.Trading.Operations.SignificantAveragingSell;
using Outcompute.Trader.Trading.Operations.TrackingBuy;
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
        public abstract Task<IAlgoResult> GoAsync(CancellationToken cancellationToken = default);

        public virtual Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public virtual Task StopAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public IAlgoContext Context { get; set; } = NullAlgoContext.Instance;

        public virtual NoopAlgoResult Noop()
        {
            return NoopAlgoResult.Instance;
        }

        public virtual ManyAlgoResult Many(IEnumerable<IAlgoResult> results)
        {
            return new ManyAlgoResult(results);
        }

        public virtual ManyAlgoResult Many(params IAlgoResult[] results)
        {
            return new ManyAlgoResult(results);
        }

        public virtual AveragingSellAlgoResult AveragingSell(Symbol symbol, IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings)
        {
            return new AveragingSellAlgoResult(symbol, orders, profitMultiplier, redeemSavings);
        }

        public virtual CreateOrderAlgoResult CreateOrder(Symbol symbol, OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag)
        {
            return new CreateOrderAlgoResult(symbol, type, side, timeInForce, quantity, price, tag);
        }

        public virtual CancelOrderAlgoResult CancelOrder(Symbol symbol, long orderId)
        {
            return new CancelOrderAlgoResult(symbol, orderId);
        }

        public virtual EnsureSingleOrderAlgoResult EnsureSingleOrder(Symbol symbol, OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings)
        {
            return new EnsureSingleOrderAlgoResult(symbol, side, type, timeInForce, quantity, price, redeemSavings);
        }

        public virtual ClearOpenOrdersAlgoResult ClearOpenOrders(Symbol symbol, OrderSide side)
        {
            return new ClearOpenOrdersAlgoResult(symbol, side);
        }

        public virtual RedeemSavingsAlgoResult TryRedeemSavings(string asset, decimal amount)
        {
            return new RedeemSavingsAlgoResult(asset, amount);
        }

        public virtual SignificantAveragingSellAlgoResult SignificantAveragingSell(Symbol symbol, MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings)
        {
            return new SignificantAveragingSellAlgoResult(symbol, ticker, orders, minimumProfitRate, redeemSavings);
        }

        public virtual TrackingBuyAlgoResult TrackingBuy(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings)
        {
            return new TrackingBuyAlgoResult(symbol, pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, redeemSavings);
        }
    }
}