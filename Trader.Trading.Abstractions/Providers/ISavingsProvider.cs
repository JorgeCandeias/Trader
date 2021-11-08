﻿using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ISavingsProvider
    {
        Task<SavingsPosition?> TryGetPositionAsync(string asset, CancellationToken cancellation = default);

        Task<SavingsQuota?> TryGetQuotaAsync(string asset, CancellationToken cancellationToken = default);

        Task<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default);
    }
}