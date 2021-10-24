using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks.AveragingSell
{
    public class AveragingSellResult : IAlgoResult
    {
        public AveragingSellResult(Symbol symbol, IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Orders = orders ?? throw new ArgumentNullException(nameof(orders));
            ProfitMultiplier = profitMultiplier;
            RedeemSavings = redeemSavings;
        }

        public Symbol Symbol { get; }
        public IReadOnlyCollection<OrderQueryResult> Orders { get; }
        public decimal ProfitMultiplier { get; }
        public bool RedeemSavings { get; }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoResultExecutor<AveragingSellResult>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}