using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    [Reentrant]
    [StatelessWorker(1)]
    internal class KlineProviderReplicaGrain : Grain, IKlineProviderReplicaGrain
    {
        private readonly KlineProviderOptions _options;
        private readonly ReactiveOptions _reactive;
        private readonly IAlgoDependencyInfo _dependencies;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IGrainFactory _factory;

        public KlineProviderReplicaGrain(IOptions<KlineProviderOptions> options, IOptions<ReactiveOptions> reactive, IAlgoDependencyInfo dependencies, IHostApplicationLifetime lifetime, IGrainFactory factory)
        {
            _options = options.Value;
            _reactive = reactive.Value;
            _dependencies = dependencies;
            _lifetime = lifetime;
            _factory = factory;
        }

        /// <summary>
        /// The symbol that this grain is responsible for.
        /// </summary>
        private string _symbol = null!;

        /// <summary>
        /// The interval that this grain instance is reponsible for.
        /// </summary>
        private KlineInterval _interval;

        /// <summary>
        /// The current version.
        /// </summary>
        private Guid _version;

        /// <summary>
        /// The current change serial number;
        /// </summary>
        private int _serial;

        /// <summary>
        /// Maximum cached periods needed by algos.
        /// </summary>
        private int _periods;

        /// <summary>
        /// Holds the kline cache in a form that is mutable but still convertible to immutable upon request with low overhead.
        /// </summary>
        private readonly ImmutableSortedSet<Kline>.Builder _klines = ImmutableSortedSet.CreateBuilder(Kline.OpenTimeComparer);

        /// <summary>
        /// Indexes klines by open time to speed up requests for a single order.
        /// </summary>
        private readonly Dictionary<DateTime, Kline> _klineByOpenTime = new();

        public override async Task OnActivateAsync()
        {
            (_symbol, _interval) = this.GetPrimaryKeys();

            _periods = _dependencies
                .GetKlines(_symbol, _interval)
                .Select(x => x.Periods)
                .DefaultIfEmpty(0)
                .Max();

            await LoadAsync();

            RegisterTimer(_ => PollAsync(), null, _reactive.ReactiveRecoveryDelay, _reactive.ReactiveRecoveryDelay);

            RegisterTimer(_ => ClearAsync(), null, _options.CleanupPeriod, _options.CleanupPeriod);

            await base.OnActivateAsync();
        }

        public Task<Kline?> TryGetKlineAsync(DateTime openTime)
        {
            var kline = _klineByOpenTime.TryGetValue(openTime, out var current) ? current : null;

            return Task.FromResult(kline);
        }

        public Task<IReadOnlyList<Kline>> GetKlinesAsync()
        {
            return Task.FromResult<IReadOnlyList<Kline>>(_klines.ToImmutable());
        }

        public async Task SetKlineAsync(Kline item)
        {
            await GrainFactory.GetKlineProviderGrain(_symbol, _interval).SetKlineAsync(item);

            Apply(item);
        }

        public async Task SetKlinesAsync(IEnumerable<Kline> items)
        {
            await GrainFactory.GetKlineProviderGrain(_symbol, _interval).SetKlinesAsync(items);

            foreach (var item in items)
            {
                Apply(item);
            }
        }

        public Task<DateTime?> TryGetLastOpenTimeAsync()
        {
            return Task.FromResult(_klines.Max?.OpenTime);
        }

        private async Task LoadAsync()
        {
            var result = await GrainFactory.GetKlineProviderGrain(_symbol, _interval).GetKlinesAsync();

            _version = result.Version;
            _serial = result.Serial;

            foreach (var kline in result.Klines)
            {
                Apply(kline);
            }
        }

        private void Apply(Kline item)
        {
            // remove old item to allow an update
            Remove(item);

            // add new or updated item
            _klines.Add(item);

            // index the item
            Index(item);
        }

        private void Remove(Kline item)
        {
            if (_klines.Remove(item) && !Unindex(item))
            {
                throw new InvalidOperationException($"Failed to unindex kline ('{item.Symbol}','{item.Interval}','{item.OpenTime}')");
            }
        }

        private void Index(Kline item)
        {
            _klineByOpenTime[item.OpenTime] = item;
        }

        private bool Unindex(Kline item)
        {
            return _klineByOpenTime.Remove(item.OpenTime);
        }

        private async Task PollAsync()
        {
            while (!_lifetime.ApplicationStopping.IsCancellationRequested)
            {
                try
                {
                    var result = await GrainFactory
                        .GetKlineProviderGrain(_symbol, _interval)
                        .TryGetKlinesAsync(_version, _serial + 1);

                    if (result.HasValue)
                    {
                        _version = result.Value.Version;
                        _serial = result.Value.Serial;

                        foreach (var item in result.Value.Klines)
                        {
                            Apply(item);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // throw on target shutdown - allow time for recovery
                    await Task.Delay(_reactive.ReactiveRecoveryDelay, _lifetime.ApplicationStopping);
                }
            }
        }

        private Task ClearAsync()
        {
            while (_klines.Count > 0 && _klines.Count > _periods)
            {
                Remove(_klines.Min!);
            }

            return Task.CompletedTask;
        }
    }
}