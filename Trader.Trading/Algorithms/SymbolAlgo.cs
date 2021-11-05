using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Commands.SignificantAveragingSell;
using Outcompute.Trader.Trading.Commands.TrackingBuy;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public abstract class SymbolAlgo : Algo, ISymbolAlgo
    {
        public override Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            return OnExecuteAsync(cancellationToken);
        }

        protected virtual Task<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IAlgoCommand>(Noop());
        }

        private Symbol EnsureSymbol()
        {
            if (Context.Symbol == Symbol.Empty)
            {
                throw new InvalidOperationException("Current algo has no default symbol");
            }

            return Context.Symbol;
        }

        public virtual AveragingSellCommand AveragingSell(IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings, bool redeemSwapPool)
        {
            return AveragingSell(EnsureSymbol(), orders, profitMultiplier, redeemSavings, redeemSwapPool);
        }

        public virtual CreateOrderCommand CreateOrder(OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag)
        {
            return CreateOrder(EnsureSymbol(), type, side, timeInForce, quantity, price, tag);
        }

        public virtual EnsureSingleOrderCommand EnsureSingleOrder(OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings, bool redeemSwapPool)
        {
            return EnsureSingleOrder(EnsureSymbol(), side, type, timeInForce, quantity, price, redeemSavings, redeemSwapPool);
        }

        public virtual ClearOpenOrdersCommand ClearOpenOrders(OrderSide side)
        {
            return ClearOpenOrders(EnsureSymbol(), side);
        }

        public virtual SignificantAveragingSellCommand SignificantAveragingSell(MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings, bool redeemSwapPool)
        {
            return SignificantAveragingSell(EnsureSymbol(), ticker, orders, minimumProfitRate, redeemSavings, redeemSwapPool);
        }

        public virtual TrackingBuyCommand TrackingBuy(decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings)
        {
            return TrackingBuy(EnsureSymbol(), pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, redeemSavings);
        }
    }
}