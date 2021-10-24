using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.RedeemSavings
{
    public class RedeemSavingsAlgoResult : IAlgoResult
    {
        public RedeemSavingsAlgoResult(string asset, decimal amount)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Amount = amount;
        }

        public string Asset { get; }
        public decimal Amount { get; }

        public Task<RedeemSavingsOperationResult> ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoResultExecutor<RedeemSavingsAlgoResult, RedeemSavingsOperationResult>>()
                .ExecuteAsync(context, this, cancellationToken);
        }

        Task IAlgoResult.ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken)
        {
            return ExecuteAsync(context, cancellationToken);
        }
    }
}