using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Dashboard;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Trader.Dashboard.Tests
{
    public class TraderDashboardServiceTests
    {
        [Fact]
        public async Task Cycles()
        {
            // arrange
            var options = new TraderDashboardOptions();
            var loggerProviders = Enumerable.Empty<ILoggerProvider>();
            var provider = Mock.Of<IServiceProvider>();
            using var service = new TraderDashboardService(Options.Create(options), loggerProviders, provider);

            // act
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);

            // assert
            Assert.True(true);
        }
    }
}