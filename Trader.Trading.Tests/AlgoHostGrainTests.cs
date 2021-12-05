using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Outcompute.Trader.Tests.Fakes;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Readyness;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoHostGrainTests
    {
        [Fact]
        public async Task Pings()
        {
            // arrange
            var logger = NullLogger<AlgoHostGrain>.Instance;
            var options = Mock.Of<IOptionsMonitor<AlgoOptions>>(x => x.Get("Algo1").Type == "AlgoType1");
            var context = Mock.Of<IGrainActivationContext>(x => x.GrainIdentity.PrimaryKeyString == "Algo1");
            var readyness = Mock.Of<IReadynessProvider>();
            var publisher = Mock.Of<IAlgoStatisticsPublisher>();
            var algo = Mock.Of<IAlgo>();
            var resolver = Mock.Of<IAlgoFactoryResolver>(x => x.Resolve("AlgoType1").Create("Algo1") == algo);

            var provider = new ServiceCollection()
                .AddScoped(_ => resolver)
                .BuildServiceProvider();

            var timers = new FakeTimerRegistry();

            // activate
            using var grain = new AlgoHostGrain(logger, options, context, readyness, publisher, provider, timers);
            await grain.OnActivateAsync();

            // act
            await grain.PingAsync();

            // assert
            Assert.True(true);
        }
    }
}