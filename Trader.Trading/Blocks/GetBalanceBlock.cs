using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class GetBalanceBlock
    {
        /// <summary>
        /// Gets the balance for the specified <paramref name="asset"/>.
        /// Throws <see cref="InvalidOperationException"/> if no balance is found.
        /// </summary>
        public static ValueTask<Balance> GetRequiredBalanceAsync(this IAlgoContext context, string asset, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            var repository = context.ServiceProvider.GetRequiredService<ITradingRepository>();

            return GetRequiredBalanceInnerAsync(asset, repository, cancellationToken);
        }

        private static async ValueTask<Balance> GetRequiredBalanceInnerAsync(string asset, ITradingRepository repository, CancellationToken cancellationToken = default)
        {
            var result = await repository.TryGetBalanceAsync(asset, cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                throw new InvalidOperationException($"Could not get balance for asset '{asset}'");
            }

            return result;
        }

        /// <summary>
        /// Gets the balance for the specified <paramref name="asset"/>.
        /// Returns <see cref="Balance.Zero(string)"/> for the specified asset if no balance is found.
        /// </summary>
        public static ValueTask<Balance> GetBalanceOrDefaultAsync(this IAlgoContext context, string asset, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            var repository = context.ServiceProvider.GetRequiredService<ITradingRepository>();

            return GetBalanceOrDefaultInnerAsync(asset, repository, cancellationToken);
        }

        private static async ValueTask<Balance> GetBalanceOrDefaultInnerAsync(string asset, ITradingRepository repository, CancellationToken cancellationToken = default)
        {
            var result = await repository.TryGetBalanceAsync(asset, cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                return Balance.Zero(asset);
            }

            return result;
        }
    }
}