using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Trading.Binance.Handlers;
using System.Net;

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
    }
}