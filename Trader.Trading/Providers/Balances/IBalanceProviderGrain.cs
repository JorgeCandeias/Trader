using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Balances
{
    internal interface IBalanceProviderGrain : IGrainWithStringKey
    {
        Task<ReactiveResult> GetBalanceAsync();

        Task<ReactiveResult?> TryWaitForBalanceAsync(Guid version);

        Task<Balance?> TryGetBalanceAsync();

        Task SetBalanceAsync(Balance balance);
    }
}