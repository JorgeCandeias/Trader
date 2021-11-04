using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Timers;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Swap
{
    internal sealed class SwapPoolGrain : Grain, ISwapPoolGrain, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly ITimerRegistry _timers;
        private readonly IBalanceProvider _balances;

        public SwapPoolGrain(ILogger<SwapPoolGrain> logger, ITradingService trader, ITimerRegistry timers, IBalanceProvider balances)
        {
            _logger = logger;
            _trader = trader;
            _timers = timers;
            _balances = balances;
        }

        private static string TypeName => nameof(SwapPoolGrain);

        private readonly CancellationTokenSource _cancellation = new();

        private readonly Dictionary<long, SwapPool> _pools = new();

        private readonly Dictionary<long, SwapPoolLiquidity> _liquidities = new();

        private readonly Dictionary<long, SwapPoolConfiguration> _configurations = new();

        private readonly Dictionary<long, DateTime> _readCooldowns = new();

        private readonly Dictionary<long, DateTime> _writeCooldowns = new();

        public override async Task OnActivateAsync()
        {
            await LoadAsync();

            _timers.RegisterTimer(this, _ => TickAsync(), null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            await base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _cancellation.Cancel();

            return base.OnDeactivateAsync();
        }

        private async Task TickAsync()
        {
            await LoadAsync();

            // get all balances for which there are pools
            var balances = new Dictionary<string, Balance>();
            foreach (var asset in _configurations.Values.SelectMany(x => x.Assets.Keys).Distinct())
            {
                balances[asset] = await _balances.GetBalanceOrZeroAsync(asset, _cancellation.Token);
            }

            // for each pool attempt to allocate balances
            /*
            foreach (var pool in _configurations)
            {
                // add to the pool
                _trader.AddSwapLiquidityAsync(pool.Key, SwapPoolLiquidityType.Combination, )
            }
            */

            // get all pools for which we have enough balance to add liquidity to
            //_configurations.Values.Where(c => c.Assets.All(a => balances.TryGetValue(a.Key, out var balance) && balance.Free >= a.Value.MinAdd))
        }

        public Task PingAsync() => Task.CompletedTask;

        public void Dispose()
        {
            _cancellation.Dispose();
        }

        private async Task LoadAsync()
        {
            await LoadSwapPoolsAsync();
            await LoadSwapPoolLiquiditiesAsync();
            await LoadSwapPoolConfigurationsAsync();
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

        private async Task LoadSwapPoolConfigurationsAsync()
        {
            var watch = Stopwatch.StartNew();

            var configurations = await _trader.GetSwapPoolConfigurationsAsync(_cancellation.Token);

            _configurations.ReplaceWith(configurations, x => x.PoolId);

            _logger.LogInformation("{Type} loaded {Count} Swap Pool Configuration details in {ElapsedMs}ms", TypeName, _configurations.Count, watch.ElapsedMilliseconds);
        }
    }
}