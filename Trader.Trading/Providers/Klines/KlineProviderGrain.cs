using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using OrleansDashboard;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    [Reentrant]
    internal class KlineProviderGrain : Grain, IKlineProviderGrain
    {
        private readonly ReactiveOptions _reactive;
        private readonly ITradingRepository _repository;
        private readonly IAlgoDependencyInfo _dependencies;
        private readonly ISystemClock _clock;
        private readonly IHostApplicationLifetime _lifetime;

        public KlineProviderGrain(IOptions<ReactiveOptions> reactive, ITradingRepository repository, IAlgoDependencyInfo dependencies, ISystemClock clock, IHostApplicationLifetime lifetime)
        {
            _reactive = reactive.Value;
            _repository = repository;
            _dependencies = dependencies;
            _clock = clock;
            _lifetime = lifetime;
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
        private Guid _version = Guid.NewGuid();

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

        /// <summary>
        /// Assigns a unique serial number to each kline.
        /// </summary>
        private readonly Dictionary<Kline, int> _serialByKline = new(Kline.OpenTimeEqualityComparer);

        /// <summary>
        /// Indexes each kline by it serial number.
        /// </summary>
        private readonly Dictionary<int, Kline> _klineBySerial = new();

        /// <summary>
        /// Tracks all reactive caching requests.
        /// </summary>
        private readonly Dictionary<(Guid Version, int FromSerial), TaskCompletionSource<ReactiveResult?>> _completions = new();

        public override async Task OnActivateAsync()
        {
            (_symbol, _interval) = this.GetPrimaryKeys();

            _periods = _dependencies
                .GetKlines(_symbol, _interval)
                .Select(x => x.Periods)
                .DefaultIfEmpty(0)
                .Max();

            await LoadAsync();

            await base.OnActivateAsync();
        }

        public Task<Kline?> TryGetKlineAsync(DateTime openTime)
        {
            var kline = _klineByOpenTime.TryGetValue(openTime, out var current) ? current : null;

            return Task.FromResult(kline);
        }

        public Task<ReactiveResult> GetKlinesAsync()
        {
            return Task.FromResult(new ReactiveResult(_version, _serial, _klines.ToImmutable()));
        }

        [NoProfiling]
        public Task<ReactiveResult?> TryWaitForKlinesAsync(Guid version, int fromSerial)
        {
            // if the versions differ then return the entire data set
            if (version != _version)
            {
                return Task.FromResult<ReactiveResult?>(new ReactiveResult(_version, _serial, _klines.ToImmutable()));
            }

            // fulfill the request now if possible
            if (_serial >= fromSerial)
            {
                var builder = ImmutableSortedSet.CreateBuilder(Kline.OpenTimeComparer);

                for (var i = fromSerial; i <= _serial; i++)
                {
                    if (_klineBySerial.TryGetValue(i, out var kline))
                    {
                        builder.Add(kline);
                    }
                }

                return Task.FromResult<ReactiveResult?>(new ReactiveResult(_version, _serial, builder.ToImmutable()));
            }

            // otherwise let the request wait for more data
            return GetOrCreateCompletionTask(version, fromSerial).WithDefaultOnTimeout(null, _reactive.ReactivePollingTimeout, _lifetime.ApplicationStopping);
        }

        private async Task LoadAsync()
        {
            var result = await _repository.GetKlinesAsync(_symbol, _interval, _clock.UtcNow.Subtract(_interval, _periods + 1), _clock.UtcNow);

            foreach (var kline in result)
            {
                Apply(kline);
            }
        }

        public async Task SetKlineAsync(Kline item)
        {
            await _repository.SetKlineAsync(item);

            Apply(item);
        }

        public async Task SetKlinesAsync(IEnumerable<Kline> items)
        {
            await _repository.SetKlinesAsync(items);

            foreach (var item in items)
            {
                Apply(item);
            }
        }

        private void Apply(Kline item)
        {
            // remove old item to allow an update
            if (_klines.Remove(item) && !Unindex(item))
            {
                throw new InvalidOperationException($"Failed to unindex kline ('{item.Symbol}','{item.Interval}','{item.OpenTime}')");
            }

            _klines.Add(item);

            Index(item);
            Complete();
        }

        private void Index(Kline item)
        {
            _klineByOpenTime[item.OpenTime] = item;
            _serialByKline[item] = ++_serial;
            _klineBySerial[_serial] = item;
        }

        private bool Unindex(Kline item)
        {
            return
                _klineByOpenTime.Remove(item.OpenTime) &&
                _serialByKline.Remove(item, out var serial) &&
                _klineBySerial.Remove(serial);
        }

        private void Complete()
        {
            // break early if there is nothing to complete
            if (_completions.Count == 0) return;

            // elect promises for completion
            var elected = ArrayPool<(Guid Version, int FromSerial)>.Shared.Rent(_completions.Count);
            var count = 0;
            foreach (var key in _completions.Keys)
            {
                if (key.Version != _version || key.FromSerial <= _serial)
                {
                    elected[count++] = key;
                }
            }

            // remove and complete elected promises
            for (var i = 0; i < count; i++)
            {
                var key = elected[i];

                if (_completions.Remove(key, out var completion))
                {
                    if (key.Version != _version)
                    {
                        // complete on data reset
                        completion.SetResult(new ReactiveResult(_version, _serial, _klines.ToImmutable()));
                    }
                    else
                    {
                        // complete on changes only
                        var builder = ImmutableSortedSet.CreateBuilder(Kline.OpenTimeComparer);

                        for (var s = key.FromSerial; s <= _serial; s++)
                        {
                            if (_klineBySerial.TryGetValue(s, out var kline))
                            {
                                builder.Add(kline);
                            }
                        }

                        completion.SetResult(new ReactiveResult(_version, _serial, builder.ToImmutable()));
                    }
                }
            }

            // cleanup
            ArrayPool<(Guid, int)>.Shared.Return(elected);
        }

        private Task<ReactiveResult?> GetOrCreateCompletionTask(Guid version, int fromSerial)
        {
            if (!_completions.TryGetValue((version, fromSerial), out var completion))
            {
                _completions[(version, fromSerial)] = completion = new TaskCompletionSource<ReactiveResult?>();
            }

            return completion.Task;
        }
    }
}