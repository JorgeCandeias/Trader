using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.TestingHost;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Data.InMemory.Tests.Fixtures
{
    public sealed class ClusterFixture : IDisposable
    {
        public ClusterFixture()
        {
            Cluster = new TestClusterBuilder()
                .AddSiloBuilderConfigurator<HostConfigurator>()
                .AddSiloBuilderConfigurator<TestSiloConfigurator>()
                .Build();

            Cluster.Deploy();
        }

        public TestCluster Cluster { get; }

        public void Dispose()
        {
            Cluster.StopAllSilos();
            Cluster.Dispose();
        }
    }

    public class HostConfigurator : IHostConfigurator
    {
        public void Configure(IHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "Trader:Algos:MyTestAlgo:Type", "Test" },
                        { "Trader:Algos:MyTestAlgo:Options:TestValue", "123" },
                    });
                });
        }
    }

    public class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .AddTrader()
                .AddInMemoryTradingRepository()
                .ConfigureServices(services =>
                {
                    services
                        .AddAlgoType<TestAlgo>("Test")
                        .AddAlgoOptionsType<TestAlgoOptions>();
                });
        }
    }

    public class TestAlgo : Algo
    {
        protected override ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IAlgoCommand>(Noop());
        }
    }

    public class TestAlgoOptions
    {
        public int TestValue { get; set; }
    }
}