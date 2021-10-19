using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    internal class KlineProviderGrain : Grain, IKlineProviderGrain
    {
        private readonly KlineProviderOptions _options;
        private readonly ITradingRepository _repository;
        private readonly IHostApplicationLifetime _lifetime;

        public KlineProviderGrain(IOptions<KlineProviderOptions> options, ITradingRepository repository, IHostApplicationLifetime lifetime)
        {
            _options = options.Value;
            _repository = repository;
            _lifetime = lifetime;
        }

        /// <summary>
        /// Holds the kline cache in a form that is mutable but still convertible to immutable upon request with low overhead.
        /// </summary>
        private readonly ImmutableSortedSet<Kline>.Builder _klines = ImmutableSortedSet.CreateBuilder(Kline.OpenTimeComparer);

        /// <summary>
        /// An instance version that helps reset replicas upon reactivation of this grain.
        /// </summary>
        private readonly Guid _version = Guid.NewGuid();

        /// <summary>
        /// Helps tag all incoming klines so we push minimal diffs to replicas.
        /// </summary>
        private int _serial;

        /// <summary>
        /// Tag the last serial number that was saved to the repository.
        /// </summary>
        private int _savedSerial;

        /// <summary>
        /// Assigns a unique serial number to all klines.
        /// </summary>
        private readonly Dictionary<Kline, int> _serialByKline = new(Kline.OpenTimeEqualityComparer);

        /// <summary>
        /// Indexes klines by their latest serial number to speed up update requests.
        /// </summary>
        private readonly Dictionary<int, Kline> _klineBySerial = new();

        /// <summary>
        /// Indexes klines by their open time to speed up requests for a single kline.
        /// </summary>
        private readonly Dictionary<DateTime, Kline> _klineByOpenTime = new();

        /// <summary>
        /// Symbol that this grain instance is responsible for.
        /// </summary>
        private string _symbol = string.Empty;

        /// <summary>
        /// Interval that this grain instance is responsible for.
        /// </summary>
        private KlineInterval _interval = KlineInterval.None;

        public override async Task OnActivateAsync()
        {
            var keys = this.GetPrimaryKeyString().Split('|');
            _symbol = keys[0];
            _interval = Enum.Parse<KlineInterval>(keys[1], false);

            await LoadAsync();

            RegisterTimer(TickSaveKlinesAsync, null, _options.SavePeriod, _options.SavePeriod);

            RegisterTimer(TickCleanupAsync, null, _options.CleanupPeriod, _options.CleanupPeriod);

            await base.OnActivateAsync();
        }

        public Task<(Guid Version, int MaxSerial, IReadOnlyList<Kline> Klines)> GetKlinesAsync()
        {
            return Task.FromResult<(Guid Version, int MaxSerial, IReadOnlyList<Kline> Kline)>((_version, _serial, _klines.ToImmutable()));
        }

        public Task SetKlineAsync(Kline kline)
        {
            SetKlineCore(kline);

            return Task.CompletedTask;
        }

        public Task SetKlinesAsync(IEnumerable<Kline> klines)
        {
            foreach (var item in klines)
            {
                SetKlineCore(item);
            }

            return Task.CompletedTask;
        }

        public Task<Kline?> TryGetKlineAsync(DateTime openTime)
        {
            var kline = _klineByOpenTime.TryGetValue(openTime, out var current) ? current : null;

            return Task.FromResult<Kline?>(kline);
        }

        public Task<(Guid Version, int MaxSerial, IReadOnlyList<Kline> Klines)> TryGetKlinesAsync(Guid version, int fromSerial)
        {
            // if the version is different then return all the klines
            if (version != _version)
            {
                return GetKlinesAsync();
            }

            // if there is nothing to return then return an empty collection
            if (fromSerial > _serial)
            {
                return Task.FromResult<(Guid Version, int MaxSerial, IReadOnlyList<Kline> Klines)>((_version, _serial, ImmutableSortedSet<Kline>.Empty));
            }

            // otherwise return all new klines
            var builder = ImmutableSortedSet.CreateBuilder(Kline.OpenTimeComparer);
            for (var serial = fromSerial; serial <= _serial; serial++)
            {
                if (_klineBySerial.TryGetValue(serial, out var order))
                {
                    builder.Add(order);
                }
            }
            return Task.FromResult<(Guid Version, int MaxSerial, IReadOnlyList<Kline> Klines)>((_version, _serial, builder.ToImmutable()));
        }

        private void SetKlineCore(Kline item)
        {
            if (item.Symbol != _symbol || item.Interval != _interval)
            {
                throw new InvalidOperationException($"{nameof(KlineProviderGrain)} for ('{_symbol}','{_interval}') cannot accept kline for ('{item.Symbol}','{item.OpenTime}')");
            }

            RemoveKlineCore(item);
            AddKlineCore(item);
        }

        private void RemoveKlineCore(Kline item)
        {
            // remove and unindex the old version
            if (_klines.Remove(item) && !(_klineByOpenTime.Remove(item.OpenTime, out _) && _serialByKline.Remove(item, out var serial) && _klineBySerial.Remove(serial)))
            {
                throw new InvalidOperationException($"Failed to unindex order '{item.OpenTime}'");
            }
        }

        private void AddKlineCore(Kline item)
        {
            // keep the new order
            if (!_klines.Add(item))
            {
                throw new InvalidOperationException($"Failed to add kline for '{item.OpenTime}'");
            }

            // index the new order
            _klineByOpenTime[item.OpenTime] = item;
            _serialByKline[item] = ++_serial;
            _klineBySerial[_serial] = item;
        }

        private Task TickSaveKlinesAsync(object _)
        {
            // break early if there is nothing to save
            if (_savedSerial == _serial) return Task.CompletedTask;

            // go on the async path only if there are items to save
            return TickSaveKlinesCoreAsync();
        }

        /// <summary>
        /// Saves all unsaved items from the cache to the repository.
        /// </summary>
        private async Task TickSaveKlinesCoreAsync()
        {
            // pin the current serial as it can change by interleaving tasks
            var maxSerial = _serial;

            // elect orders to save
            var elected = ArrayPool<Kline>.Shared.Rent(maxSerial - _savedSerial + 1);
            var count = 0;

            for (var serial = _savedSerial + 1; serial <= maxSerial; ++serial)
            {
                if (_klineBySerial.TryGetValue(serial, out var order))
                {
                    elected[count++] = order;
                }
            }

            // save the items
            await _repository.SetKlinesAsync(elected.Take(count), _lifetime.ApplicationStopping);

            // mark the max serial as saved now
            _savedSerial = maxSerial;

            // cleanup
            ArrayPool<Kline>.Shared.Return(elected);
        }

        /// <summary>
        /// Loads all orders from the repository into the cache for the current symbol.
        /// </summary>
        private async Task LoadAsync()
        {
            var orders = await _repository.GetKlinesAsync(_symbol, _interval, DateTime.MinValue, DateTime.MaxValue, _lifetime.ApplicationStopping);

            await SetKlinesAsync(orders);
        }

        /// <summary>
        /// Cleans up old klines from the cache.
        /// </summary>
        private Task TickCleanupAsync(object _)
        {
            while (_klines.Count > _options.MaxCachedKlines)
            {
                _klines.Remove(_klines.Min!);
            }

            return Task.CompletedTask;
        }
    }
}