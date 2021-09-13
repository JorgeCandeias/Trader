using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Exchange
{
    internal class ExchangeInfoGrain : Grain, IExchangeInfoGrain
    {
        private readonly ITradingService _trader;

        public ExchangeInfoGrain(ITradingService trader)
        {
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
        }

        private ExchangeInfo _info = ExchangeInfo.Empty;
        private Guid _version = Guid.NewGuid();

        public override async Task OnActivateAsync()
        {
            await RefreshExchangeInfoAsync().ConfigureAwait(true);

            RegisterTimer(_ => RefreshExchangeInfoAsync(), null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            await base.OnActivateAsync().ConfigureAwait(true);
        }

        private async Task RefreshExchangeInfoAsync()
        {
            _info = await _trader
                .GetExchangeInfoAsync()
                .ConfigureAwait(true);

            _version = Guid.NewGuid();
        }

        public Task<(ExchangeInfo, Guid)> GetExchangeInfoAsync()
        {
            return Task.FromResult((_info, _version));
        }

        public Task<(ExchangeInfo?, Guid)> TryGetNewExchangeInfoAsync(Guid version)
        {
            // if the client has the latest version then return nothing
            if (version == _version)
            {
                return Task.FromResult<(ExchangeInfo?, Guid)>((null, version));
            }

            // otherwise return the latest version
            return Task.FromResult<(ExchangeInfo?, Guid)>((_info, _version));
        }
    }
}