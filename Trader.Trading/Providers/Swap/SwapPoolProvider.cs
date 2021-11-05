using Orleans;
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

        public Task<decimal> GetBalanceAsync(string asset, CancellationToken cancellationToken = default)
        {
            return _factory.GetSwapPoolGrain().GetBalanceAsync(asset);
        }
    }
}