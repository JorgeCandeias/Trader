using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
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

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    [Reentrant]
    [StatelessWorker(1)]
    internal class BinanceKlineProviderGrain : Grain, IBinanceKlineProviderGrain
    {
        private readonly ILogger _logger;
        private readonly ITradingRepository _repository;
        private readonly IAlgoDependencyInfo _dependencies;
        private readonly ISystemClock _clock;

        public BinanceKlineProviderGrain(ILogger<BinanceKlineProviderGrain> logger, ITradingRepository repository, IAlgoDependencyInfo dependencies, ISystemClock clock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Helps keep the logs neat.
        /// </summary>
        private static string TypeName => nameof(BinanceKlineProviderGrain);

        /// <summary>
        /// Symbol key component.
        /// </summary>
        private string _symbol = string.Empty;

        /// <summary>
        /// Interval key component.
        /// </summary>
        private KlineInterval _interval;

        /// <summary>
        /// The cached periods for the current data set.
        /// </summary>
        private int _periods;

        /// <summary>
        /// Holds the local kline cache.
        /// </summary>
        private readonly Dictionary<DateTime, Kline> _cache = new();

        public override async Task OnActivateAsync()
        {
            // decompose the grain key
            var key = this.GetPrimaryKeyString().Split('|');
            _symbol = key[0];
            _interval = Enum.Parse<KlineInterval>(key[1]);

            // take the maximum window from all the algos that need the current kline set
            _periods = _dependencies
                .GetKlines(_symbol, _interval)
                .DefaultIfEmpty(KlineDependency.Empty)
                .Max(x => x.Periods);

            // load and validate the initial data set
            await LoadAsync();

            // keep updating the data set in the background
            RegisterTimer(UpdateAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            // keep cleaning up expired klines in the background
            RegisterTimer(TickCleanupAsync, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            await base.OnActivateAsync();
        }

        /// <summary>
        /// Loads and validates the initial data set.
        /// </summary>
        private async Task LoadAsync()
        {
            // skip if there are no periods to load
            if (_periods <= 0) return;

            // load the existing klines from the repository
            var end = _clock.UtcNow;
            var start = end.Subtract(_interval, _periods);
            var klines = await _repository.GetKlinesAsync(_symbol, _interval, start, end);
            foreach (var kline in klines)
            {
                _cache[kline.OpenTime] = kline;
            }

            // validate that we have all klines needed by the current window
            foreach (var time in _interval.Range(start, end))
            {
                if (!_cache.ContainsKey(time))
                {
                    throw new KeyNotFoundException($"Could not load kline for (Symbol = '{_symbol}', Interval = '{_interval}', OpenTime = '{time}')");
                }
            }
        }

        /// <summary>
        /// Updates missing or open klines in the background.
        /// </summary>
        private async Task UpdateAsync(object _)
        {
            // skip if there are no periods to load
            if (_periods <= 0) return;

            // load the new klines from the repository
            var end = _clock.UtcNow;
            var start = end.Subtract(_interval, _periods);

            // enumerate all needed kline timestamps
            foreach (var time in _interval.Range(start, end))
            {
                // check if we have the final closed kline
                if (!_cache.TryGetValue(time, out var kline) || !kline.IsClosed)
                {
                    // attempt to get a new or updated kline from the repository
                    kline = await _repository.TryGetKlineAsync(_symbol, _interval, time);

                    // issue a warning if we cannot load the kline
                    if (kline is null)
                    {
                        _logger.LogWarning(
                            "{TypeName} {Symbol} {Interval} could not load required kline for {OpenTime}",
                            TypeName, _symbol, _interval, time);

                        continue;
                    }

                    // otherwise keep the kline
                    _cache[time] = kline;
                }
            }
        }

        private Task TickCleanupAsync(object _)
        {
            // prepare to elect expired keys for removal
            var start = _clock.UtcNow.Subtract(_interval, _periods);
            var buffer = ArrayPool<DateTime>.Shared.Rent(_cache.Count);
            var count = 0;

            // elect expired items for removal
            foreach (var key in _cache.Keys)
            {
                if (count >= buffer.Length)
                {
                    break;
                }

                if (key < start)
                {
                    buffer[count++] = key;
                }
            }

            // remove expired items
            for (var i = 0; i < count; i++)
            {
                _cache.Remove(buffer[i]);
            }

            // cleanup
            ArrayPool<DateTime>.Shared.Return(buffer);

            return Task.CompletedTask;
        }

        public async Task<IReadOnlyCollection<Kline>> GetKlinesAsync(DateTime start, DateTime end)
        {
            var builder = ImmutableSortedSet.CreateBuilder(Kline.KeyComparer);

            foreach (var time in _interval.Range(start, end))
            {
                // attempt to get the kline from cache
                if (!_cache.TryGetValue(time, out var kline))
                {
                    // if the kline is not cached then attempt to get it from the repository
                    kline = await _repository.TryGetKlineAsync(_symbol, _interval, time);

                    // if the kline does not exist at all then we cant fullfill this request
                    if (kline is null)
                    {
                        throw new KeyNotFoundException($"Could not provide kline for (Symbol = '{_symbol}', Interval = '{_interval}', OpenTime = '{time}')");
                    }

                    // otherwise keep it to save work for the next caller
                    _cache[kline.OpenTime] = kline;
                }

                builder.Add(kline);
            }

            return builder.ToImmutable();
        }
    }
}