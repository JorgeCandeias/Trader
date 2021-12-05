using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Tests.Fakes;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoManagerGrainTests
    {
        [Fact]
        public async Task GetsAlgos()
        {
            // arrange
            var options = new TraderOptions
            {
                PingDelay = TimeSpan.FromSeconds(1),
                Algos =
                {
                    ["MyTestAlgo"] = new AlgoOptions
                    {
                        Type = "Test",
                        Enabled = true,
                        MaxExecutionTime = TimeSpan.FromMinutes(1),
                        TickDelay = TimeSpan.FromSeconds(10),
                        TickEnabled = true
                    }
                }
            };

            var monitor = Mock.Of<IOptionsMonitor<TraderOptions>>(x => x.CurrentValue == options);
            var logger = NullLogger<AlgoManagerGrain>.Instance;
            var lifetime = Mock.Of<IHostApplicationLifetime>();
            var timers = new FakeTimerRegistry();
            using var grain = new AlgoManagerGrain(monitor, logger, lifetime, timers);

            // activate
            await grain.OnActivateAsync();

            // act
            var result = await grain.GetAlgosAsync();

            var info = result.First();
            Assert.Equal("MyTestAlgo", info.Name);
            Assert.Equal("Test", info.Type);
            Assert.True(info.Enabled);
            Assert.Equal(TimeSpan.FromMinutes(1), info.MaxExecutionTime);
            Assert.Equal(TimeSpan.FromSeconds(10), info.TickDelay);
            Assert.True(info.TickEnabled);
        }
    }
}