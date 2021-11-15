﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    [Reentrant]
    [StatelessWorker(1)]
    internal class KlineProviderReplicaGrain : Grain, IKlineProviderReplicaGrain
    {
        private readonly KlineProviderOptions _options;
        private readonly ReactiveOptions _reactive;
        private readonly IAlgoDependencyResolver _dependencies;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IGrainFactory _factory;
        private readonly ITradingRepository _repository;

        public KlineProviderReplicaGrain(IOptions<KlineProviderOptions> options, IOptions<ReactiveOptions> reactive, IAlgoDependencyResolver dependencies, IHostApplicationLifetime lifetime, IGrainFactory factory, ITradingRepository repository)
        {
            _options = options.Value;
            _reactive = reactive.Value;
            _dependencies = dependencies;
            _lifetime = lifetime;
            _factory = factory;
            _repository = repository;
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
        private readonly ImmutableSortedSet<Kline>.Builder _klines = ImmutableSortedSet.CreateBuilder(KlineComparer.Key);

        /// <summary>
        /// Indexes klines by open time to speed up requests for a single order.
        /// </summary>
        private readonly Dictionary<DateTime, Kline> _klineByOpenTime = new();

        public override async Task OnActivateAsync()
        {
            (_symbol, _interval) = this.GetPrimaryKeys();

            _periods = _dependencies.Klines.TryGetValue((_symbol, _interval), out var periods) ? periods : 0;

            await LoadAsync();

            RegisterTimer(_ => PollAsync(), null, _reactive.ReactiveRecoveryDelay, _reactive.ReactiveRecoveryDelay);

            RegisterTimer(_ => ClearAsync(), null, _options.CleanupPeriod, _options.CleanupPeriod);

            await base.OnActivateAsync();
        }

        public ValueTask<Kline?> TryGetKlineAsync(DateTime openTime)
        {
            if (_klineByOpenTime.TryGetValue(openTime, out var kline))
            {
                return ValueTask.FromResult<Kline?>(kline);
            }

            return ValueTask.FromResult<Kline?>(null);
        }

        public ValueTask<IReadOnlyList<Kline>> GetKlinesAsync()
        {
            return ValueTask.FromResult<IReadOnlyList<Kline>>(_klines.ToImmutable());
        }

        public ValueTask<IReadOnlyList<Kline>> GetKlinesAsync(DateTime tickTime, int periods)
        {
            var result = _klines.Reverse().SkipWhile(x => x.OpenTime > tickTime).Take(periods).ToImmutableSortedSet(KlineComparer.Key);

            return ValueTask.FromResult<IReadOnlyList<Kline>>(result);
        }

        public async ValueTask SetKlineAsync(Kline item)
        {
            await _repository.SetKlineAsync(item, _lifetime.ApplicationStopping);

            await _factory.GetKlineProviderGrain(_symbol, _interval).SetKlineAsync(item);

            Apply(item);
        }

        public async ValueTask SetKlinesAsync(IEnumerable<Kline> items)
        {
            await _repository.SetKlinesAsync(items, _lifetime.ApplicationStopping);

            await _factory.GetKlineProviderGrain(_symbol, _interval).SetKlinesAsync(items);

            foreach (var item in items)
            {
                Apply(item);
            }
        }

        public ValueTask<DateTime?> TryGetLastOpenTimeAsync()
        {
            if (_klines.Count > 0)
            {
                return ValueTask.FromResult<DateTime?>(_klines.Max.OpenTime);
            }

            return ValueTask.FromResult<DateTime?>(null);
        }

        private async Task LoadAsync()
        {
            var result = await _factory.GetKlineProviderGrain(_symbol, _interval).GetKlinesAsync();

            _version = result.Version;
            _serial = result.Serial;

            foreach (var item in result.Items)
            {
                Apply(item);
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
                        .TryWaitForKlinesAsync(_version, _serial + 1);

                    if (result.HasValue)
                    {
                        _version = result.Value.Version;
                        _serial = result.Value.Serial;

                        foreach (var item in result.Value.Items)
                        {
                            Apply(item);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // throw on target shutdown
                    return;
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