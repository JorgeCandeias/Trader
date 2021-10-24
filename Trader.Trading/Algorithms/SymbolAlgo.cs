using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Operations.AveragingSell;
using Outcompute.Trader.Trading.Operations.ClearOpenOrders;
using Outcompute.Trader.Trading.Operations.CreateOrder;
using Outcompute.Trader.Trading.Operations.EnsureSingleOrder;
using Outcompute.Trader.Trading.Operations.SignificantAveragingSell;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public abstract class SymbolAlgo : Algo, ISymbolAlgo
    {
        public override Task<IAlgoResult> GoAsync(CancellationToken cancellationToken = default)
        {
            return OnExecuteAsync(cancellationToken);
        }

        protected virtual Task<IAlgoResult> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IAlgoResult>(Noop());
        }

        private Symbol EnsureSymbol()
        {
            if (Context.Symbol == Symbol.Empty)
            {
                throw new InvalidOperationException("Current algo has no default symbol");
            }

            return Context.Symbol;
        }

        public virtual AveragingSellAlgoResult AveragingSell(IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings)
        {
            return AveragingSell(EnsureSymbol(), orders, profitMultiplier, redeemSavings);
        }

        public virtual CreateOrderAlgoResult CreateOrder(OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag)
        {
            return CreateOrder(EnsureSymbol(), type, side, timeInForce, quantity, price, tag);
        }

        public virtual EnsureSingleOrderAlgoResult EnsureSingleOrder(OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings)
        {
            return EnsureSingleOrder(EnsureSymbol(), side, type, timeInForce, quantity, price, redeemSavings);
        }

        public virtual ClearOpenOrdersAlgoResult ClearOpenOrders(OrderSide side)
        {
            return ClearOpenOrders(EnsureSymbol(), side);
        }

        public virtual Task<IReadOnlyList<OrderQueryResult>> GetOpenOrdersAsync(OrderSide side, CancellationToken cancellationToken = default)
        {
            return GetOpenOrdersAsync(EnsureSymbol(), side, cancellationToken);
        }

        public virtual SignificantAveragingSellAlgoResult SignificantAveragingSell(MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings)
        {
            return SignificantAveragingSell(EnsureSymbol(), ticker, orders, minimumProfitRate, redeemSavings);
        }

        public virtual Task<bool> SetTrackingBuyAsync(decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            return SetTrackingBuyAsync(EnsureSymbol(), pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, redeemSavings, cancellationToken);
        }
    }
}