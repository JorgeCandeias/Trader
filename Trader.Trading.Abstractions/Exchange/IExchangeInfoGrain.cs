using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Exchange
{
    public interface IExchangeInfoGrain : IGrainWithGuidKey
    {
        Task<(ExchangeInfo Info, Guid Version)> GetExchangeInfoAsync();

        Task<(ExchangeInfo? Info, Guid Version)> TryGetNewExchangeInfoAsync(Guid version);
    }
}