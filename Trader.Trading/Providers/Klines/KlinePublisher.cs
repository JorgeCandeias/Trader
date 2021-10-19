using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Models;
using Polly;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    internal class KlinePublisher : IKlinePublisher
    {
        private readonly KlineProviderOptions _options;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IGrainFactory _factory;

        public KlinePublisher(IOptions<KlineProviderOptions> options, ILogger<KlinePublisher> logger, IHostApplicationLifetime lifetime, IGrainFactory factory)
        {
            _options = options.Value;
            _logger = logger;
            _lifetime = lifetime;
            _factory = factory;
        }

        /// <summary>
        /// Holds the partioned kline propagation channels.
        /// </summary>
        private readonly ConcurrentDictionary<(string Symbol, KlineInterval Interval), (Channel<Kline> Channel, Task Consumer)> _channels = new();

        private Channel<Kline> EnsureChannel(string symbol, KlineInterval interval)
        {
            var result = _channels.GetOrAdd((symbol, interval), _ =>
            {
                var channel = Channel.CreateUnbounded<Kline>(new UnboundedChannelOptions
                {
                    AllowSynchronousContinuations = false,
                    SingleReader = true,
                    SingleWriter = false
                });

                var task = ConsumeTask(symbol, interval, channel);

                return (channel, task);
            });

            return result.Channel;
        }

        private async Task ConsumeTask(string symbol, KlineInterval interval, Channel<Kline> channel)
        {
            while (await channel.Reader
                .WaitToReadAsync(_lifetime.ApplicationStopping)
                .ConfigureAwait(false))
            {
                var buffer = ArrayPool<Kline>.Shared.Rent(channel.Reader.Count);
                var count = 0;

                while (count < buffer.Length && channel.Reader.TryRead(out var item))
                {
                    buffer[count++] = item;
                }

                var elected = buffer.Take(count);

                await Policy
                    .Handle<Exception>()
                    .WaitAndRetryForeverAsync(
                        _ => _options.PublishRetryDelay,
                        (ex, delay) =>
                        {
                            _logger.LogError(ex,
                                "{Name} failed to publish klines and will retry in {Delay}",
                                nameof(KlinePublisher), delay);
                        })
                    .ExecuteAsync(
                        ct => _factory.GetKlineProviderGrain(symbol, interval).SetKlinesAsync(elected),
                        _lifetime.ApplicationStopping,
                        false)
                    .ConfigureAwait(false);

                ArrayPool<Kline>.Shared.Return(buffer);
            }
        }

        public void Publish(Kline kline)
        {
            if (kline is null) throw new ArgumentNullException(nameof(kline));

            var channel = EnsureChannel(kline.Symbol, kline.Interval);

            if (!channel.Writer.TryWrite(kline))
            {
                _logger.LogError(
                    "{Name} could not write kline {Kline} to channel",
                    nameof(KlinePublisher), kline);
            }
        }
    }
}