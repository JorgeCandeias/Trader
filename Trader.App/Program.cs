using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Trader.App
{
    internal class Program
    {
        protected Program()
        {
        }

        private static Task Main()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddUserSecrets<Program>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog(new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .Filter.ByExcluding(x => x.Properties.TryGetValue("SourceContext", out var property) && property is ScalarValue scalar && scalar.Value.Equals("System.Net.Http.HttpClient.BinanceApiClient.ClientHandler") && x.Level < LogEventLevel.Warning)
                        .Filter.ByExcluding(x => x.Properties.TryGetValue("SourceContext", out var property) && property is ScalarValue scalar && scalar.Value.Equals("System.Net.Http.HttpClient.BinanceApiClient.LogicalHandler") && x.Level < LogEventLevel.Warning)
                        .WriteTo.Console()
                        .CreateLogger(), true);
                })
                .ConfigureServices((context, services) =>
                {
                    services
                        .AddModelServices()
                        .AddAlgorithmResolvers()
                        .AddBinanceTradingService(options => context.Configuration.Bind("Api", options))
                        .AddMarketDataStreamHost(options =>
                        {
                            options.Symbols.UnionWith(context
                                .Configuration
                                .GetSection("Trading:Algorithms")
                                .GetChildren()
                                .SelectMany(x => x.GetChildren())
                                .Select(x => x["Symbol"]));
                        })
                        .AddUserDataStreamHost(options =>
                        {
                            options.Symbols.UnionWith(context
                                .Configuration
                                .GetSection("Trading:Algorithms")
                                .GetChildren()
                                .SelectMany(x => x.GetChildren())
                                .Select(x => x["Symbol"]));
                        })
                        .AddTradingHost(options => context.Configuration.Bind("Trading:Host"))
                        .AddSystemClock()
                        .AddSafeTimerFactory()
                        .AddSqlTraderRepository(options =>
                        {
                            options.ConnectionString = context.Configuration.GetConnectionString("Trader");
                        })
                        .AddBase62NumberSerializer();

                    // add all algorithms
                    foreach (var type in context.Configuration.GetSection("Trading:Algorithms").GetChildren())
                    {
                        switch (type.Key)
                        {
                            case "Accumulator":
                                foreach (var algo in type.GetChildren())
                                {
                                    services.AddAccumulatorAlgorithm(algo.Key, options => algo.Bind(options));
                                }
                                break;

                            case "Step":
                                foreach (var algo in type.GetChildren())
                                {
                                    services.AddStepAlgorithm(algo.Key, options => algo.Bind(options));
                                }
                                break;

                            default:
                                throw new InvalidOperationException($"Unknown algorithm type '{type.Key}'");
                        }
                    }
                })
                .RunConsoleAsync();
        }
    }
}