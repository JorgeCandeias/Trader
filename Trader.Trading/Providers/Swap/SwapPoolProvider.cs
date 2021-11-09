using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Swap
{
    internal class SwapPoolProvider : ISwapPoolProvider
    {
        private readonly IGrainFactory _factory;

        public SwapPoolProvider(IGrainFactory factory)
        {
            _factory = factory;
        }

        public Task<RedeemSwapPoolEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default)
        {
            return _factory.GetSwapPoolGrain().RedeemAsync(asset, amount);
        }

        public Task<SwapPoolAssetBalance> GetBalanceAsync(string asset, CancellationToken cancellationToken = default)
        {
            return _factory.GetSwapPoolGrain().GetBalanceAsync(asset);
        }

        public Task<IEnumerable<SwapPool>> GetSwapPoolsAsync(CancellationToken cancellationToken = default)
        {
            return _factory.GetSwapPoolGrain().GetSwapPoolsAsync();
        }

        public Task<IEnumerable<SwapPoolConfiguration>> GetSwapPoolConfigurationsAsync(CancellationToken cancellationToken = default)
        {
            return _factory.GetSwapPoolGrain().GetSwapPoolConfigurationsAsync();
        }
    }
}