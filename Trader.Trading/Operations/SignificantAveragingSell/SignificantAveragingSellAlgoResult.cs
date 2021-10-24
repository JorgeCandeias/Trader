using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.SignificantAveragingSell
{
    public class SignificantAveragingSellAlgoResult : IAlgoResult
    {
        public SignificantAveragingSellAlgoResult(Symbol symbol, MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
            Orders = orders ?? throw new ArgumentNullException(nameof(orders));
            MinimumProfitRate = minimumProfitRate;
            RedeemSavings = redeemSavings;
        }

        public Symbol Symbol { get; }
        public MiniTicker Ticker { get; }
        public IReadOnlyCollection<OrderQueryResult> Orders { get; }
        public decimal MinimumProfitRate { get; }
        public bool RedeemSavings { get; }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoResultExecutor<SignificantAveragingSellAlgoResult>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}