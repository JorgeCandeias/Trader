using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using Outcompute.Trader.Core.Timers;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    internal sealed class KlinePublisher : IKlinePublisher, IHostedService, IDisposable
    {
        private readonly TraderStreamOptions _options;
        private readonly ILogger _logger;
        private readonly IClusterClient _client;
        private readonly ISafeTimerFactory _timers;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ITradingRepository _repository;

        public KlinePublisher(IOptions<TraderStreamOptions> options, ILogger<KlinePublisher> logger, IClusterClient client, ISafeTimerFactory timers, IHostApplicationLifetime lifetime, ITradingRepository repository)
        {
            _options = options.Value;
            _logger = logger;
            _client = client;
            _timers = timers;
            _lifetime = lifetime;
            _repository = repository;
        }

        private readonly Channel<Kline> _items = Channel.CreateUnbounded<Kline>();

        private Task? _work;

        private ISafeTimer? _timer;

        private async Task TickEnsureWork(CancellationToken cancellationToken)
        {
            // spin up streaming work if not running
            if (_work is null)
            {
                _work = Task.Run(async () =>
                {
                    await foreach (var item in _items.Reader.ReadAllAsync(_lifetime.ApplicationStopping))
                    {
                        try
                        {
                            await _repository
                                .SetKlineAsync(item, cancellationToken)
                                .ConfigureAwait(false);

                            await _client
                                .GetStreamProvider(_options.StreamProviderName)
                                .GetKlineStream(item.Symbol, item.Interval)
                                .OnNextAsync(item)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "{Name} failed to publish kline {Kline} and will enqueue it again for processing on recovery",
                                nameof(KlinePublisher), item);

                            await _items.Writer
                                .WriteAsync(item, _lifetime.ApplicationStopping)
                                .ConfigureAwait(false);

                            throw;
                        }
                    }
                }, CancellationToken.None);

                return;
            }

            // bubble up streaming exceptions and reset
            if (_work.IsCompleted)
            {
                try
                {
                    await _work;
                }
                finally
                {
                    _work = null;
                }
            }
        }

        public ValueTask PublishAsync(Kline item, CancellationToken cancellationToken = default)
        {
            return _items.Writer.WriteAsync(item, cancellationToken);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = _timers.Create(TickEnsureWork, _options.StreamRecoveryPeriod, _options.StreamRecoveryPeriod, Timeout.InfiniteTimeSpan);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}