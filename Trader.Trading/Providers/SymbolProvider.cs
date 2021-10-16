using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Exchange;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    internal class SymbolProvider : ISymbolProvider
    {
        private readonly IExchangeInfoReplicaGrain _grain;

        public SymbolProvider(IGrainFactory factory)
        {
            _grain = factory.GetExchangeInfoReplicaGrain();
        }

        public ValueTask<Symbol?> TryGetSymbolAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return _grain.TryGetSymbolAsync(symbol);
        }
    }
}