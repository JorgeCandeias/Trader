using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers.Exchange;
using System.Collections.Immutable;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class ExchangeInfoProviderTests
    {
        [Fact]
        public async Task GetsExchangeInfo()
        {
            // arrange
            var symbol = Symbol.Empty with { Name = "ABCXYZ" };
            var info = ExchangeInfo.Empty with { Symbols = ImmutableList.Create(symbol) };
            var options = Options.Create(new ExchangeInfoOptions());
            var logger = NullLogger<ExchangeInfoProvider>.Instance;
            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IExchangeInfoGrain>(Guid.Empty, null).GetExchangeInfoAsync())
                .ReturnsAsync(new ExchangeInfoResult(info, Guid.NewGuid()))
                .Verifiable();

            using var provider = new ExchangeInfoProvider(options, logger, factory);

            // act
            await provider.StartAsync(CancellationToken.None);
            var result = provider.GetExchangeInfo();

            // assert
            Assert.NotNull(result);
            Assert.Same(info, result);
            Mock.Get(factory).VerifyAll();
        }
    }
}