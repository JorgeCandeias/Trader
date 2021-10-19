using Orleans;
using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Exchange
{
    internal class ExchangeInfoProvider : IExchangeInfoProvider
    {
        public IExchangeInfoReplicaGrain _grain;

        public ExchangeInfoProvider(IGrainFactory factory)
        {
            _grain = factory.GetExchangeInfoReplicaGrain();
        }

        public Task<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default)
        {
            return _grain.GetExchangeInfoAsync();
        }

        public Task<Symbol?> TryGetSymbolAsync(string name, CancellationToken cancellationToken = default)
        {
            return _grain.TryGetSymbolAsync(name);
        }
    }
}