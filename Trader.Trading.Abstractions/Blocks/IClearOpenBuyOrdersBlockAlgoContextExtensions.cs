using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Blocks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class IClearOpenBuyOrdersBlockAlgoContextExtensions
    {
        /// <inheritdoc cref="IClearOpenBuyOrdersBlock.GoAsync(Symbol, CancellationToken)"/>
        public static ValueTask ClearOpenBuyOrdersAsync(this IAlgoContext context, Symbol symbol, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            var block = context.ServiceProvider.GetRequiredService<IClearOpenBuyOrdersBlock>();

            return block.GoAsync(symbol, cancellationToken);
        }
    }
}