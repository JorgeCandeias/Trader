using Orleans;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Exchange
{
    internal interface IExchangeInfoGrain : IGrainWithGuidKey
    {
        Task<ExchangeInfoResult> GetExchangeInfoAsync();

        Task<ExchangeInfoTryResult> TryGetExchangeInfoAsync(Guid version);
    }
}