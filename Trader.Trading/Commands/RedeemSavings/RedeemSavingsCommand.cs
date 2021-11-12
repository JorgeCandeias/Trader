using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.RedeemSavings
{
    public class RedeemSavingsCommand : IAlgoCommand
    {
        public RedeemSavingsCommand(string asset, decimal amount)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Amount = amount;
        }

        public string Asset { get; }
        public decimal Amount { get; }

        public ValueTask<RedeemSavingsEvent> ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoCommandExecutor<RedeemSavingsCommand, RedeemSavingsEvent>>()
                .ExecuteAsync(context, this, cancellationToken);
        }

        async ValueTask IAlgoCommand.ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken)
        {
            await ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }
}