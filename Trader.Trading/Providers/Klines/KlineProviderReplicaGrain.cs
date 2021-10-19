using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    [Reentrant]
    [StatelessWorker(1)]
    internal class KlineProviderReplicaGrain : Grain, IKlineProviderReplicaGrain
    {
        private readonly KlineProviderOptions _options;
        private readonly IGrainFactory _factory;
        private readonly IHostApplicationLifetime _lifetime;

        public KlineProviderReplicaGrain(IOptions<KlineProviderOptions> options, IGrainFactory factory, IHostApplicationLifetime lifetime)
        {
            _options = options.Value;
            _factory = factory;
            _lifetime = lifetime;
        }

        /// <summary>
        /// The symbol that this grain is responsible for.
        /// </summary>
        private string _symbol = Empty;

        /// <summary>
        /// The interval that this grain instance is reponsible for.
        /// </summary>
        private KlineInterval _interval = KlineInterval.None;

        /// <summary>
        /// The serial version of this grain.
        /// Helps detect serial resets from the source grain.
        /// </summary>
        private Guid _version;

        /// <summary>
        /// The last known change serial.
        /// </summary>
        private int _serial;

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
            var keys = this.GetPrimaryKeyString().Split('|');
            _symbol = keys[0];
            _interval = Enum.Parse<KlineInterval>(keys[1], false);

            await LoadAsync();

            RegisterTimer(TickUpdateAsync, null, _options.PropagationPeriod, _options.PropagationPeriod);

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

        public async Task SetKlinesAsync(IEnumerable<Kline> klines)
        {
            // let the main grain handle saving so it updates every other replica
            await _factory.GetKlineProviderGrain(_symbol, _interval).SetKlinesAsync(klines);

            // apply the klines to this replica now so they are consistent from the point of view of the algo calling this method
            // the updated serial numbers will eventually come through as reactive caching calls resolve
            Apply(klines);
        }

        public async Task SetKlineAsync(Kline kline)
        {
            // let the main grain handle saving so it updates every other replica
            await _factory.GetKlineProviderGrain(_symbol, _interval).SetKlineAsync(kline);

            // apply the klines to this replica now so they are consistent from the point of view of the algo calling this method
            // the updated serial numbers will eventually come through as reactive caching calls resolve
            Apply(kline);
        }

        private async Task LoadAsync()
        {
            var result = await _factory.GetKlineProviderGrain(_symbol, _interval).GetKlinesAsync();

            Apply(result.Klines);
        }

        private Task TickUpdateAsync(object _)
        {
            // perform sync checks
            if (_lifetime.ApplicationStopping.IsCancellationRequested) return Task.CompletedTask;

            // go on the async path
            return TickUpdateCoreAsync();
        }

        private async Task TickUpdateCoreAsync()
        {
            try
            {
                var result = await _factory.GetKlineProviderGrain(_symbol, _interval).TryGetKlinesAsync(_version, _serial + 1);

                Apply(result.Version, result.MaxSerial, result.Klines);
            }
            catch (OperationCanceledException)
            {
                // noop - happens at target shutdown
            }
        }

        private void Apply(Guid version, int serial, IEnumerable<Kline> klines)
        {
            Apply(version, serial);

            foreach (var kline in klines)
            {
                Apply(kline);
            }
        }

        private void Apply(Guid version, int serial)
        {
            _version = version;
            _serial = serial;
        }

        private void Apply(IEnumerable<Kline> klines)
        {
            foreach (var kline in klines)
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