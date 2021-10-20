using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;
using Orleans.Runtime;
using Orleans.Streams;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
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
    [PreferLocalPlacement]
    internal class KlineProviderReplicaGrain : Grain, IKlineProviderReplicaGrain
    {
        private readonly TraderStreamOptions _options;
        private readonly ILogger _logger;
        private readonly ILocalSiloDetails _details;
        private readonly ITradingRepository _repository;
        private readonly IAlgoDependencyInfo _dependencies;
        private readonly ISystemClock _clock;
        private readonly IHostApplicationLifetime _lifetime;

        public KlineProviderReplicaGrain(IOptions<TraderStreamOptions> options, ILogger<KlineProviderReplicaGrain> logger, ILocalSiloDetails details, ITradingRepository repository, IAlgoDependencyInfo dependencies, ISystemClock clock, IHostApplicationLifetime lifetime)
        {
            _options = options.Value;
            _logger = logger;
            _details = details;
            _repository = repository;
            _dependencies = dependencies;
            _clock = clock;
            _lifetime = lifetime;
        }

        /// <summary>
        /// The target silo address of this replica.
        /// </summary>
        private SiloAddress _address = null!;

        /// <summary>
        /// The symbol that this grain is responsible for.
        /// </summary>
        private string _symbol = null!;

        /// <summary>
        /// The interval that this grain instance is reponsible for.
        /// </summary>
        private KlineInterval _interval;

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
            (_address, _symbol, _interval) = this.GetPrimaryKeys();

            if (_address != _details.SiloAddress)
            {
                _logger.LogWarning(
                    "{Name} {Symbol} {Interval} instance for silo '{Address}' activated in wrong silo '{SiloAddress}' and will deactivate to allow relocation",
                    nameof(KlineProviderReplicaGrain), _symbol, _interval, _address, _details.SiloAddress);

                RegisterTimer(_ => { DeactivateOnIdle(); return Task.CompletedTask; }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            _periods = _dependencies
                .GetKlines(_symbol, _interval)
                .Select(x => x.Periods)

                .DefaultIfEmpty(0)

                .Max();

            await SubscribeAsync();

            await LoadAsync();

            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            await UnsubscribeAsync();

            await base.OnDeactivateAsync();
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

        #region Streaming

        private async Task SubscribeAsync()
        {
            var stream = GetStreamProvider(_options.StreamProviderName).GetKlineStream(_symbol, _interval);
            var subs = await stream.GetAllSubscriptionHandles();

            if (subs.Count > 0)
            {
                await subs[0].ResumeAsync(OnNextAsync);
                foreach (var sub in subs.Skip(1))
                {
                    await sub.UnsubscribeAsync();
                }
                return;
            }

            await stream.SubscribeAsync(OnNextAsync);
        }

        private async Task UnsubscribeAsync()
        {
            var stream = GetStreamProvider(_options.StreamProviderName).GetKlineStream(_symbol, _interval);

            foreach (var sub in await stream.GetAllSubscriptionHandles())
            {
                await sub.UnsubscribeAsync();
            }
        }

        public Task OnNextAsync(Kline item, StreamSequenceToken? token = null)
        {
            Apply(item);

            return Task.CompletedTask;
        }

        #endregion Streaming

        private async Task LoadAsync()
        {
            var result = await _repository.GetKlinesAsync(_symbol, _interval, _clock.UtcNow.Subtract(_interval, _periods + 1), _clock.UtcNow);

            foreach (var kline in result)
            {
                Apply(kline);
            }
        }

        private void Apply(Kline kline)
        {
            // remove old item to allow an update
            if (_klines.Remove(kline) && !_klineByOpenTime.Remove(kline.OpenTime))
            {
                throw new InvalidOperationException($"Failed to unindex kline ('{kline.Symbol}','{kline.Interval}','{kline.OpenTime}')");
            }

            // add new or updated item
            _klines.Add(kline);

            // index the item
            _klineByOpenTime[kline.OpenTime] = kline;
        }
    }
}