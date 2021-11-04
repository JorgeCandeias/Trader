using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Timers;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Swap
{
    internal sealed class SwapGrain : Grain, ISwapGrain, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly ITimerRegistry _timers;

        public SwapGrain(ILogger<SwapGrain> logger, ITradingService trader, ITimerRegistry timers)
        {
            _logger = logger;
            _trader = trader;
            _timers = timers;
        }

        private static string TypeName => nameof(SwapGrain);

        private readonly CancellationTokenSource _cancellation = new();

        private readonly Dictionary<long, SwapPool> _pools = new();

        private readonly Dictionary<long, SwapPoolLiquidity> _liquidities = new();

        public override async Task OnActivateAsync()
        {
            _timers.RegisterTimer(this, _ => TickAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            await LoadSwapPoolsAsync();
            await LoadSwapPoolLiquiditiesAsync();

            await base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _cancellation.Cancel();

            return base.OnDeactivateAsync();
        }

        private Task TickAsync()
        {
            return Task.CompletedTask;
        }

        public Task PingAsync() => Task.CompletedTask;

        public void Dispose()
        {
            _cancellation.Dispose();
        }

        private async Task LoadSwapPoolsAsync()
        {
            var watch = Stopwatch.StartNew();

            var pools = await _trader.GetSwapPoolsAsync(_cancellation.Token);

            _pools.ReplaceWith(pools, x => x.PoolId);

            _logger.LogInformation("{Type} loaded {Count} Swap Pools in {ElapsedMs}ms", TypeName, _pools.Count, watch.ElapsedMilliseconds);
        }

        private async Task LoadSwapPoolLiquiditiesAsync()
        {
            var watch = Stopwatch.StartNew();

            var liquidities = await _trader.GetSwapLiquiditiesAsync(_cancellation.Token);

            _liquidities.ReplaceWith(liquidities, x => x.PoolId);

            _logger.LogInformation("{Type} loaded {Count} Swap Pool Liquidity details in {ElapsedMs}ms", TypeName, _liquidities.Count, watch.ElapsedMilliseconds);
        }
    }
}