using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Blocks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class IAveragingSellBlockAlgoContextExtensions
    {
        public static Task SetAveragingSellAsync(this IAlgoContext context, Symbol symbol, decimal profitMultiplier, bool useSavings, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            var block = context.ServiceProvider.GetRequiredService<IAveragingSellBlock>();

            return block.GoAsync(symbol, profitMultiplier, useSavings, cancellationToken);
        }
    }
}