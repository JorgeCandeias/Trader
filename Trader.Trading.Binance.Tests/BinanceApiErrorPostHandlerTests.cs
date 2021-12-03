using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Trading.Binance.Handlers;
using System.Net;
using System.Net.Http.Headers;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    public class BinanceApiErrorPostHandlerTests
    {
        private class BinanceApiErrorPostHandlerTester : BinanceApiErrorPostHandler
        {
            public BinanceApiErrorPostHandlerTester(IOptions<BinanceOptions> options, ILogger<BinanceApiErrorPostHandler> logger, ISystemClock clock) : base(options, logger, clock)
            {
            }

            public new Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return base.SendAsync(request, cancellationToken);
            }
        }

        private class FakeMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _action;

            public FakeMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> action)
            {
                _action = action;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_action(request));
            }
        }

        [Fact]
        public async Task HandlesSuccess()
        {
            // arrange
            var options = Mock.Of<IOptions<BinanceOptions>>();
            var logger = NullLogger<BinanceApiErrorPostHandler>.Instance;
            var clock = Mock.Of<ISystemClock>();
            using var handler = new BinanceApiErrorPostHandlerTester(options, logger, clock);
            using var request = new HttpRequestMessage();
            using var response = new HttpResponseMessage(HttpStatusCode.OK);
            handler.InnerHandler = new FakeMessageHandler(x =>
            {
                Assert.Same(request, x);
                return response;
            });

            // act
            var result = await handler.SendAsync(request, CancellationToken.None);

            // assert
            Assert.Same(response, result);
        }

        [Fact]
        public async Task HandlesTooManyRequestsWithDate()
        {
            // arrange
            var options = Mock.Of<IOptions<BinanceOptions>>(x => x.Value.DefaultBackoffPeriod == TimeSpan.FromSeconds(10));
            var logger = NullLogger<BinanceApiErrorPostHandler>.Instance;
            var now = DateTime.UtcNow;
            var clock = Mock.Of<ISystemClock>(x => x.UtcNow == now);
            var retryAfter = now.AddSeconds(10);
            using var handler = new BinanceApiErrorPostHandlerTester(options, logger, clock);
            using var request = new HttpRequestMessage();
            using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter);
            handler.InnerHandler = new FakeMessageHandler(x =>
            {
                Assert.Same(request, x);
                return response;
            });

            // act
            var result = await Assert.ThrowsAsync<BinanceTooManyRequestsException>(async () => await handler.SendAsync(request, CancellationToken.None));

            // assert
            Assert.Equal(retryAfter.Subtract(now).Add(TimeSpan.FromSeconds(1)), result.RetryAfter);
        }

        [Fact]
        public async Task HandlesTooManyRequestsWithDelta()
        {
            // arrange
            var options = Mock.Of<IOptions<BinanceOptions>>(x => x.Value.DefaultBackoffPeriod == TimeSpan.FromSeconds(10));
            var logger = NullLogger<BinanceApiErrorPostHandler>.Instance;
            var now = DateTime.UtcNow;
            var clock = Mock.Of<ISystemClock>(x => x.UtcNow == now);
            var retryAfter = TimeSpan.FromSeconds(10);
            using var handler = new BinanceApiErrorPostHandlerTester(options, logger, clock);
            using var request = new HttpRequestMessage();
            using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter);
            handler.InnerHandler = new FakeMessageHandler(x =>
            {
                Assert.Same(request, x);
                return response;
            });

            // act
            var result = await Assert.ThrowsAsync<BinanceTooManyRequestsException>(async () => await handler.SendAsync(request, CancellationToken.None));

            // assert
            Assert.Equal(retryAfter.Add(TimeSpan.FromSeconds(1)), result.RetryAfter);
        }

        [Fact]
        public async Task HandlesBinanceError()
        {
            // arrange
            var options = Mock.Of<IOptions<BinanceOptions>>(x => x.Value.DefaultBackoffPeriod == TimeSpan.FromSeconds(123));
            var logger = NullLogger<BinanceApiErrorPostHandler>.Instance;
            var now = DateTime.UtcNow;
            var clock = Mock.Of<ISystemClock>(x => x.UtcNow == now);
            using var handler = new BinanceApiErrorPostHandlerTester(options, logger, clock);
            using var request = new HttpRequestMessage();
            using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(@"
                {
                    ""code"": 123,
                    ""msg"": ""test""
                }")
            };
            handler.InnerHandler = new FakeMessageHandler(x =>
            {
                Assert.Same(request, x);
                return response;
            });

            // act
            var result = await Assert.ThrowsAsync<BinanceCodeException>(async () => await handler.SendAsync(request, CancellationToken.None));

            // assert
            Assert.Equal(123, result.BinanceCode);
            Assert.Equal("test", result.Message);
        }

        [Fact]
        public async Task HandlesCommonError()
        {
            // arrange
            var options = Mock.Of<IOptions<BinanceOptions>>(x => x.Value.DefaultBackoffPeriod == TimeSpan.FromSeconds(123));
            var logger = NullLogger<BinanceApiErrorPostHandler>.Instance;
            var now = DateTime.UtcNow;
            var clock = Mock.Of<ISystemClock>(x => x.UtcNow == now);
            using var handler = new BinanceApiErrorPostHandlerTester(options, logger, clock);
            using var request = new HttpRequestMessage();
            using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{}")
            };
            handler.InnerHandler = new FakeMessageHandler(x =>
            {
                Assert.Same(request, x);
                return response;
            });

            // act
            var result = await Assert.ThrowsAsync<HttpRequestException>(async () => await handler.SendAsync(request, CancellationToken.None));

            // assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }
}