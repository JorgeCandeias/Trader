using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.EnsureSpotBalance
{
    internal class EnsureSpotBalanceCommand : IAlgoCommand
    {
        public EnsureSpotBalanceCommand(decimal value, bool redeemSavings, bool redeemSwapPools)
        {
            Guard.IsGreaterThanOrEqualTo(value, 0, nameof(value));

            Value = value;
            RedeemSavings = redeemSavings;
            RedeemSwapPools = redeemSwapPools;
        }

        public decimal Value { get; }
        public bool RedeemSavings { get; }
        public bool RedeemSwapPools { get; }

        public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            Guard.IsNotNull(context, nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IAlgoCommandExecutor<EnsureSpotBalanceCommand>>()
                .ExecuteAsync(context, this, cancellationToken);
        }
    }
}