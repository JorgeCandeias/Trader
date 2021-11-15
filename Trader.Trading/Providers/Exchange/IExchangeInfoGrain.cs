using Orleans;

namespace Outcompute.Trader.Trading.Providers.Exchange
{
    internal interface IExchangeInfoGrain : IGrainWithGuidKey
    {
        ValueTask<ExchangeInfoResult> GetExchangeInfoAsync();

        ValueTask<ExchangeInfoTryResult> TryGetExchangeInfoAsync(Guid version);
    }
}