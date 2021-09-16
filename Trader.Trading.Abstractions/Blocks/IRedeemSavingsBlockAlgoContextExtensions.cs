using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Blocks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class IRedeemSavingsBlockAlgoContextExtensions
    {
        public static Task<bool> TryRedeemSavingsAsync(this IAlgoContext context, string asset, decimal amount, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            var block = context.ServiceProvider.GetRequiredService<IRedeemSavingsBlock>();

            return block.GoAsync(asset, amount, cancellationToken);
        }
    }
}