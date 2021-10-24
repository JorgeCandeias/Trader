using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Blocks.AveragingSell;
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
            return Task.FromResult(Noop());
        }

        private Symbol EnsureSymbol()
        {
            if (Context.Symbol == Symbol.Empty)
            {
                throw new InvalidOperationException("Current algo has no default symbol");
            }

            return Context.Symbol;
        }

        public virtual IAlgoResult AveragingSell(IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings)
        {
            return new AveragingSellResult(EnsureSymbol(), orders, profitMultiplier, redeemSavings);
        }

        public virtual Task<OrderResult> CreateOrderAsync(OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag, CancellationToken cancellationToken = default)
        {
            return CreateOrderAsync(EnsureSymbol(), type, side, timeInForce, quantity, price, tag, cancellationToken);
        }

        public virtual Task<CancelStandardOrderResult> CancelOrderAsync(long orderId, CancellationToken cancellationToken = default)
        {
            return CancelOrderAsync(EnsureSymbol().Name, orderId, cancellationToken);
        }

        public virtual Task<bool> EnsureSingleOrderAsync(OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            return EnsureSingleOrderAsync(EnsureSymbol(), side, type, timeInForce, quantity, price, redeemSavings, cancellationToken);
        }

        public virtual Task ClearOpenOrdersAsync(OrderSide side, CancellationToken cancellationToken = default)
        {
            return ClearOpenOrdersAsync(EnsureSymbol(), side, cancellationToken);
        }

        public virtual Task<IReadOnlyList<OrderQueryResult>> GetOpenOrdersAsync(OrderSide side, CancellationToken cancellationToken = default)
        {
            return GetOpenOrdersAsync(EnsureSymbol(), side, cancellationToken);
        }

        public virtual Task SetSignificantAveragingSellAsync(MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            return SetSignificantAveragingSellAsync(EnsureSymbol(), ticker, orders, minimumProfitRate, redeemSavings, cancellationToken);
        }

        public virtual Task<bool> SetTrackingBuyAsync(decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            return SetTrackingBuyAsync(EnsureSymbol(), pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, redeemSavings, cancellationToken);
        }
    }
}