using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Exchange
{
    internal class ExchangeInfoProvider : IExchangeInfoProvider
    {
        public IExchangeInfoReplicaGrain _grain;

        public ExchangeInfoProvider(IGrainFactory factory)
        {
            _grain = factory.GetExchangeInfoReplicaGrain();
        }

        public ValueTask<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default)
        {
            return _grain.GetExchangeInfoAsync();
        }

        public ValueTask<Symbol?> TryGetSymbolAsync(string name, CancellationToken cancellationToken = default)
        {
            return _grain.TryGetSymbolAsync(name);
        }
    }
}