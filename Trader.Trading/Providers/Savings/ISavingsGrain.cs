﻿using Orleans;
using Outcompute.Trader.Models;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Savings
{
    internal interface ISavingsGrain : IGrainWithStringKey
    {
        Task<SavingsPosition?> TryGetPositionAsync();

        Task<SavingsQuota?> TryGetQuotaAsync(string productId, SavingsRedemptionType type);

        Task RedeemAsync(string productId, decimal amount, SavingsRedemptionType type);
    }
}