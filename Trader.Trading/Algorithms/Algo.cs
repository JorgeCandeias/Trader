using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using System;
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

        private Symbol EnsureSymbol()
        {
            if (Context.Symbol == Symbol.Empty)
            {
                throw new InvalidOperationException("Current algo has no default symbol");
            }

            return Context.Symbol;
        }

        public virtual Task SetAveragingSellAsync(IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            return SetAveragingSellAsync(EnsureSymbol(), orders, profitMultiplier, redeemSavings, cancellationToken);
        }

        public virtual Task SetAveragingSellAsync(Symbol symbol, IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            return Context.ServiceProvider
                .GetRequiredService<AveragingSellBlock>()
                .SetAveragingSellAsync(Context, symbol, orders, profitMultiplier, redeemSavings, cancellationToken);
        }
    }
}