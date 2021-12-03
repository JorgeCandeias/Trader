using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Handlers;

namespace Outcompute.Trader.Trading.Binance.Tests;

public class BinanceApiUsageHandlerTests
{
    private class BinanceApiUsageHandlerTester : BinanceApiUsageHandler
    {
        public BinanceApiUsageHandlerTester(IOptions<BinanceOptions> options, BinanceUsageContext usage, ILogger<BinanceApiUsageHandler> logger, ISystemClock clock) : base(options, usage, logger, clock)
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
    public async Task ThrowsOnUsedLimit()
    {
        // arrange
        var options = Mock.Of<IOptions<BinanceOptions>>(x =>
            x.Value.UsedRequestWeightHeaderPrefix == "X-MBX-USED-WEIGHT-");

        var usage = new BinanceUsageContext();
        usage.SetLimit(RateLimitType.RequestWeight, TimeSpan.FromMinutes(1), 10);

        var logger = NullLogger<BinanceApiUsageHandlerTester>.Instance;
        var clock = Mock.Of<ISystemClock>();
        using var handler = new BinanceApiUsageHandlerTester(options, usage, logger, clock);
        using var request = new HttpRequestMessage();

        using var response = new HttpResponseMessage();
        response.Headers.Add("X-MBX-USED-WEIGHT-1M", "11");

        handler.InnerHandler = new FakeMessageHandler(x =>
        {
            Assert.Same(request, x);
            return response;
        });

        // act
        var ex = await Assert.ThrowsAsync<BinanceTooManyRequestsException>(() => handler.SendAsync(request, CancellationToken.None));

        // assert
        Assert.Equal(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(1)), ex.RetryAfter);
    }
}