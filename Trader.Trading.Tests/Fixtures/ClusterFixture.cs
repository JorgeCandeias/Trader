using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
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
                .AddClientBuilderConfigurator<ClientConfigurator>()
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
                        { "Trader:Algos:MyTestAlgo:DependsOn:Klines:0:Symbol", "BTCGBP" },
                        { "Trader:Algos:MyTestAlgo:DependsOn:Klines:0:Interval", "Days1" },
                        { "Trader:Algos:MyTestAlgo:DependsOn:Klines:0:Periods", "100" }
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
                .AddInMemoryTradingService()
                .ConfigureServices(services =>
                {
                    services
                        .AddAlgoType<TestAlgo>("Test")
                        .AddOptionsType<TestAlgoOptions>();
                });
        }
    }

    public class ClientConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder
                .AddInMemoryTradingRepository()
                .AddInMemoryTradingService()
                .ConfigureServices(services =>
                {
                    services
                        .AddModelServices()
                        .AddTradingServices();
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