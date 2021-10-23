using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Orleans.Runtime;
using Outcompute.Trader.Core.Randomizers;
using Outcompute.Trader.Trading.Watchdog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class GrainWatchdogTests
    {
        [Fact]
        public async Task PingsEntry()
        {
            // arrange
            var options = Options.Create(new WatchdogOptions());
            var logger = NullLogger<Watchdog.WatchdogService>.Instance;

            var provider = Mock.Of<IServiceProvider>();

            var entry = Mock.Of<IWatchdogEntry>();
            Mock.Get(entry)
                .Setup(x => x.ExecuteAsync(provider, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var entries = new[] { entry };
            var lifetime = Mock.Of<IHostApplicationLifetime>(x =>
                x.ApplicationStarted == new CancellationToken(true) &&
                x.ApplicationStopping == new CancellationToken(false) &&
                x.ApplicationStopped == new CancellationToken(false));

            var oracle = Mock.Of<ISiloStatusOracle>();

            Mock.Get(oracle)
                .Setup(x => x.GetApproximateSiloStatuses(true))
                .Returns(new Dictionary<SiloAddress, SiloStatus>
                {
                    { SiloAddress.Zero, SiloStatus.Active }
                })
                .Verifiable();

            ISiloStatusListener? listener = null;
            Mock.Get(oracle)
                .Setup(x => x.SubscribeToSiloStatusEvents(It.IsAny<ISiloStatusListener>()))
                .Callback((ISiloStatusListener l) => listener = l)
                .Returns(true)
                .Verifiable();

            var random = Mock.Of<IRandomGenerator>();
            using var watchdog = new Watchdog.WatchdogService(options, logger, entries, lifetime, oracle, random, provider);

            // act
            await watchdog.StartAsync(CancellationToken.None);

            // assert
            Mock.Get(oracle).VerifyAll();

            // act
            await Task.Delay(1000);

            // assert
            Mock.Get(entry).VerifyAll();

            // act
            await watchdog.StopAsync(CancellationToken.None);
        }
    }
}