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
                        { "Trader:Algos:MyTestAlgo1:Type", "Test" },
                        { "Trader:Algos:MyTestAlgo1:Options:TestValue", "123" },
                        { "Trader:Algos:MyTestAlgo1:DependsOn:Klines:0:Symbol", "BTCGBP" },
                        { "Trader:Algos:MyTestAlgo1:DependsOn:Klines:0:Interval", "Days1" },
                        { "Trader:Algos:MyTestAlgo1:DependsOn:Klines:0:Periods", "100" },
                        { "Trader:Algos:MyTestAlgo1:DependsOn:Tickers:0", "BTCGBP" },

                        { "Trader:Algos:MyTestAlgo2:Type", "Test" },
                        { "Trader:Algos:MyTestAlgo2:Options:TestValue", "234" },
                        { "Trader:Algos:MyTestAlgo2:DependsOn:Klines:0:Symbol", "ETHGBP" },
                        { "Trader:Algos:MyTestAlgo2:DependsOn:Klines:0:Interval", "Hours1" },
                        { "Trader:Algos:MyTestAlgo2:DependsOn:Klines:0:Periods", "200" },
                        { "Trader:Algos:MyTestAlgo2:DependsOn:Tickers:0", "ETHGBP" }
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