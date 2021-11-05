using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Commands.Many;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;
using Outcompute.Trader.Trading.Commands.SignificantAveragingSell;
using Outcompute.Trader.Trading.Commands.TrackingBuy;
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
        protected Algo()
        {
            // pin the scoped context created by the factory
            Context = AlgoContext.Current;
        }

        public abstract Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default);

        public virtual Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public virtual Task StopAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public IAlgoContext Context { get; set; }

        public virtual NoopAlgoCommand Noop()
        {
            return NoopAlgoCommand.Instance;
        }

        public virtual ManyCommand Many(IEnumerable<IAlgoCommand> results)
        {
            return new ManyCommand(results);
        }

        public virtual ManyCommand Many(params IAlgoCommand[] results)
        {
            return new ManyCommand(results);
        }

        public virtual AveragingSellCommand AveragingSell(Symbol symbol, IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings, bool redeemSwapPool)
        {
            return new AveragingSellCommand(symbol, orders, profitMultiplier, redeemSavings, redeemSwapPool);
        }

        public virtual CreateOrderCommand CreateOrder(Symbol symbol, OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag)
        {
            return new CreateOrderCommand(symbol, type, side, timeInForce, quantity, price, tag);
        }

        public virtual CancelOrderCommand CancelOrder(Symbol symbol, long orderId)
        {
            return new CancelOrderCommand(symbol, orderId);
        }

        public virtual EnsureSingleOrderCommand EnsureSingleOrder(Symbol symbol, OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings, bool redeemSwapPool)
        {
            return new EnsureSingleOrderCommand(symbol, side, type, timeInForce, quantity, price, redeemSavings, redeemSwapPool);
        }

        public virtual ClearOpenOrdersCommand ClearOpenOrders(Symbol symbol, OrderSide side)
        {
            return new ClearOpenOrdersCommand(symbol, side);
        }

        public virtual RedeemSavingsCommand TryRedeemSavings(string asset, decimal amount)
        {
            return new RedeemSavingsCommand(asset, amount);
        }

        public virtual RedeemSwapPoolCommand TryRedeemSwapPoolAsync(string asset, decimal amount)
        {
            return new RedeemSwapPoolCommand(asset, amount);
        }

        public virtual SignificantAveragingSellCommand SignificantAveragingSell(Symbol symbol, MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings, bool redeemSwapPool)
        {
            return new SignificantAveragingSellCommand(symbol, ticker, orders, minimumProfitRate, redeemSavings, redeemSwapPool);
        }

        public virtual TrackingBuyCommand TrackingBuy(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings, bool redeemSwapPool)
        {
            return new TrackingBuyCommand(symbol, pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, redeemSavings, redeemSwapPool);
        }
    }
}