using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Core.Tasks.Dataflow;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    // todo: promote this to common implementation and hide it behind the ticker provider
    [Reentrant]
    [StatelessWorker(1)]
    internal class TickerConflaterGrain : Grain, ITickerConflaterGrain
    {
        private readonly ITickerProvider _provider;
        private readonly IHostApplicationLifetime _lifetime;

        public TickerConflaterGrain(ITickerProvider provider, IHostApplicationLifetime lifetime)
        {
            _provider = provider;
            _lifetime = lifetime;
        }

        private readonly ConflateChannel<MiniTicker, MiniTicker, MiniTicker> _channel = new(
            () => MiniTicker.Empty,
            (current, item) => item.EventTime >= current.EventTime ? item : current,
            current => current);

        private Task? _stream;

        public override Task OnActivateAsync()
        {
            RegisterTimer(_ => MonitorAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            return base.OnActivateAsync();
        }

        private async Task MonitorAsync()
        {
            if (_lifetime.ApplicationStopping.IsCancellationRequested)
            {
                return;
            }

            if (_stream is null)
            {
                _stream = Task.Run(StreamAsync);
                return;
            }

            if (_stream.IsCompleted)
            {
                try
                {
                    await _stream;
                }
                catch (OperationCanceledException)
                {
                    // noop
                }
                finally
                {
                    _stream = null;
                }
            }
        }

        private async Task StreamAsync()
        {
            await foreach (var result in _channel.Reader.ReadAllAsync(_lifetime.ApplicationStopping))
            {
                await _provider.SetTickerAsync(result, _lifetime.ApplicationStopping);
            }
        }

        public ValueTask PushAsync(MiniTicker item)
        {
            return _channel.Writer.WriteAsync(item, _lifetime.ApplicationStopping);
        }
    }
}