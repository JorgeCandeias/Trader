using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Exchange
{
    [StatelessWorker(1)]
    internal class ExchangeInfoReplicaGrain : Grain, IExchangeInfoReplicaGrain
    {
        private readonly IGrainFactory _factory;

        public ExchangeInfoReplicaGrain(IGrainFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private ExchangeInfo _info = ExchangeInfo.Empty;
        private Guid _version = Guid.NewGuid();
        private IDictionary<string, Symbol> _symbols = ImmutableDictionary<string, Symbol>.Empty;

        public override async Task OnActivateAsync()
        {
            await RefreshAsync();

            RegisterTimer(TickTryRefreshAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            await base.OnActivateAsync();
        }

        public ValueTask<ExchangeInfo> GetExchangeInfoAsync()
        {
            return new ValueTask<ExchangeInfo>(_info);
        }

        public ValueTask<Symbol?> TryGetSymbolAsync(string name)
        {
            if (_symbols.TryGetValue(name, out var symbol))
            {
                return new ValueTask<Symbol?>(symbol);
            }

            return new ValueTask<Symbol?>((Symbol?)null);
        }

        private async Task RefreshAsync()
        {
            (_info, _version) = await _factory.GetExchangeInfoGrain().GetExchangeInfoAsync();

            Index();
        }

        private async Task TickTryRefreshAsync(object _)
        {
            var result = await _factory.GetExchangeInfoGrain().TryGetNewExchangeInfoAsync(_version);

            if (result.Info is not null)
            {
                _info = result.Info;
                _version = result.Version;

                Index();
            }
        }

        private void Index()
        {
            _symbols = _info.Symbols.ToDictionary(x => x.Name);
        }
    }
}