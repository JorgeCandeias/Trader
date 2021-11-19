﻿using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Providers.MarketData;
using Outcompute.Trader.Trading.Providers;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    public class MarketDataStreamerTests
    {
        [Fact]
        public async Task Streams()
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // arrange
            var logger = NullLogger<MarketDataStreamer>.Instance;
            var mapper = Mock.Of<IMapper>();
            var symbol = "ABCXYZ";

            var ticker = MiniTicker.Empty with { Symbol = symbol, ClosePrice = 123m };
            var kline = Kline.Empty with { Symbol = symbol, Interval = KlineInterval.Days1, ClosePrice = 123m };

            var client = Mock.Of<IMarketDataStreamClient>();
            Mock.Get(client)
                .SetupSequence(x => x.ReceiveAsync(cancellation.Token))
                .Returns(Task.FromResult(new MarketDataStreamMessage(null, ticker, null)))
                .Returns(Task.FromResult(new MarketDataStreamMessage(null, null, kline)))
                .Returns(Task.Delay(Timeout.InfiniteTimeSpan, cancellation.Token).ContinueWith(x => new MarketDataStreamMessage(null, null, null), cancellation.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default));

            var factory = Mock.Of<IMarketDataStreamClientFactory>();
            Mock.Get(factory)
                .Setup(x => x.Create(It.IsAny<IReadOnlyCollection<string>>()))
                .Returns(client)
                .Verifiable();

            var klineProvider = Mock.Of<IKlineProvider>();

            var received = new TaskCompletionSource<MiniTicker>();
            using var reg = cancellation.Token.Register(() => received.TrySetCanceled());

            var tickerProvider = Mock.Of<ITickerProvider>();
            Mock.Get(tickerProvider)
                .Setup(x => x.SetTickerAsync(It.IsAny<MiniTicker>(), cancellation.Token))
                .Callback((MiniTicker ticker, CancellationToken ct) =>
                {
                    received.TrySetResult(ticker);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            var streamer = new MarketDataStreamer(logger, mapper, factory, klineProvider, tickerProvider);
            var tickers = new HashSet<string>(new[] { symbol });
            var klines = new HashSet<(string, KlineInterval)>(new[] { (symbol, KlineInterval.Days1) });

            // act - start streaming
            var task = streamer.StreamAsync(tickers, klines, cancellation.Token);
            await received.Task;

            // assert
            Mock.Get(tickerProvider).Verify(x => x.SetTickerAsync(ticker, cancellation.Token));
            Mock.Get(klineProvider).Verify(x => x.SetKlineAsync(kline, cancellation.Token));
            Mock.Get(factory).VerifyAll();

            cancellation.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        }
    }
}