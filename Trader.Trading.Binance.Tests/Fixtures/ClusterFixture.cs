using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Binance.Tests.Fixtures
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
                trader
                    .ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(ClusterFixture).Assembly).WithReferences())
                    .AddInMemoryTradingRepository()
                    .AddInMemoryTradingService();
            });
        }
    }
}