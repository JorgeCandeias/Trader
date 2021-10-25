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
                .AddClientBuilderConfigurator<ClientBuilderConfigurator>()
                .AddSiloBuilderConfigurator<HostConfigurator>()
                .AddSiloBuilderConfigurator<SiloConfigurator>()
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

    public class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .AddTrader()
                .ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(ClusterFixture).Assembly).WithReferences())
                .AddInMemoryTradingRepository()
                .AddInMemoryTradingService();
        }
    }

    public class ClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder
                .AddInMemoryTradingRepository()
                .AddInMemoryTradingService();
        }
    }
}