using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class TryGetBalanceBlock
    {
        public static ValueTask<Balance?> TryGetBalanceAsync(this IAlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var repository = context.ServiceProvider.GetRequiredService<ITradingRepository>();

            return TryGetBalanceInnerAsync(symbol, repository, cancellationToken);
        }

        private static async ValueTask<Balance?> TryGetBalanceInnerAsync(string asset, ITradingRepository repository, CancellationToken cancellationToken = default)
        {
            return await repository
                .TryGetBalanceAsync(asset, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}