using Orleans;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.UserData
{
    internal class BinanceOrderProvider : IOrderProvider
    {
        private readonly IBinanceUserDataGrain _grain;

        public BinanceOrderProvider(IGrainFactory factory)
        {
            _ = factory ?? throw new ArgumentNullException(nameof(factory));

            _grain = factory.GetBinanceUserDataGrain();
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        {
            return _grain.IsReadyAsync();
        }
    }
}