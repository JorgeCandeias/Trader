using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.TestingHost;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Tests.Fixtures
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
            siloBuilder.UseTrader(trader =>
            {
                trader.ConfigureServices(services =>
                {
                    services.AddAlgoType<TestAlgo, TestAlgoOptions>("Test");
                });
            });
        }
    }

    public class TestAlgo : Algo
    {
        public override ValueTask GoAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    public class TestAlgoOptions
    {
        public int TestValue { get; set; }
    }
}