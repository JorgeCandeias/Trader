using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Statistics;
using Serilog;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;
using EnvironmentName = Microsoft.Extensions.Hosting.EnvironmentName;

namespace Outcompute.Trader.App
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        protected Program()
        {
        }

        private static Task Main()
        {
            return Host.CreateDefaultBuilder()
                .UseEnvironment(EnvironmentName.Development)
                .ConfigureHostConfiguration(config =>
                {
                    config.AddEnvironmentVariables("TRADER_");
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.Sources.Clear();
                    if (context.HostingEnvironment.IsProduction())
                    {
                        config.AddEnvironmentVariables("Trader");
                        config.AddJsonFile("appsettings.production.json", false);
                        config.AddJsonFile("appsettings.local.json", true);
                    }
                    else
                    {
                        config.AddJsonFile("appsettings.json", false);
                        config.AddUserSecrets<Program>();
                    }
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog(new LoggerConfiguration()
                        .MinimumLevel.Information()

                        // ignore excess chatter from microsoft components
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .MinimumLevel.Override("System.Net.Http.HttpClient.BinanceApiClient.ClientHandler", LogEventLevel.Warning)
                        .MinimumLevel.Override("System.Net.Http.HttpClient.BinanceApiClient.LogicalHandler", LogEventLevel.Warning)
                        .MinimumLevel.Override("Orleans.Runtime.SiloControl", LogEventLevel.Warning)
                        .MinimumLevel.Override("Orleans.Runtime.Management.ManagementGrain", LogEventLevel.Warning)

                        // filter reactive caching monitoring messages until https://github.com/dotnet/orleans/issues/7330 is resolved
                        .Filter.ByExcluding(x =>
                        {
                            return x.Properties.TryGetValue("SourceContext", out var context)
                                && context is ScalarValue scalarContext
                                && (scalarContext.Value.Equals("Orleans.Runtime.InsideRuntimeClient") || scalarContext.Value.Equals("Orleans.Runtime.OutsideRuntimeClient"))
                                && x.Properties.TryGetValue("RequestMessage", out var message)
                                && message is ScalarValue scalarMessage
                                && scalarMessage.Value is string stringMessage
                                && (stringMessage.Contains("InvokeMethodRequest Outcompute.Trader.Trading.Providers.Klines.IKlineProviderGrain:TryWaitForKlinesAsync", StringComparison.Ordinal)
                                || stringMessage.Contains("InvokeMethodRequest Outcompute.Trader.Trading.Providers.Orders.IOrderProviderGrain:TryWaitForOrdersAsync", StringComparison.Ordinal)
                                || stringMessage.Contains("InvokeMethodRequest Outcompute.Trader.Trading.Providers.Tickers.ITickerProviderGrain:TryWaitForTickerAsync", StringComparison.Ordinal)
                                || stringMessage.Contains("InvokeMethodRequest Outcompute.Trader.Trading.Providers.Balances.IBalanceProviderGrain:TryWaitForBalanceAsync", StringComparison.Ordinal)
                                || stringMessage.Contains("InvokeMethodRequest Outcompute.Trader.Trading.Providers.Trades.ITradeProviderGrain:TryWaitForTradesAsync", StringComparison.Ordinal));
                        })

                        .WriteTo.Console()
                        //.WriteTo.Async(x => x.File(@"./logs/log.txt", rollingInterval: RollingInterval.Hour, rollOnFileSizeLimit: true))
                        .CreateLogger(), true);
                })
                .UseOrleans((context, orleans) =>
                {
                    orleans.ConfigureEndpoints(11111, 30000);
                    orleans.UseLinuxEnvironmentStatistics();
                    orleans.Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = nameof(Trader);
                        options.ServiceId = nameof(Trader);
                    });
                    orleans.UseAdoNetClustering(options =>
                    {
                        options.Invariant = "Microsoft.Data.SqlClient";
                        options.ConnectionString = context.Configuration.GetConnectionString("Trader");
                    });
                    orleans.UseAdoNetReminderService(options =>
                    {
                        options.Invariant = "Microsoft.Data.SqlClient";
                        options.ConnectionString = context.Configuration.GetConnectionString("Trader");
                    });
                    orleans.AddAdoNetGrainStorageAsDefault(options =>
                    {
                        options.Invariant = "Microsoft.Data.SqlClient";
                        options.ConnectionString = context.Configuration.GetConnectionString("Trader");
                    });

                    orleans.UseDashboard(options =>
                    {
                        options.Port = 6001;
                    });

                    orleans
                        .AddTrader()
                        .AddBinanceTradingService(options =>
                        {
                            context.Configuration.Bind("Binance", options);
                        })
                        .ConfigureServices((context, services) =>
                        {
                            services
                                .AddSqlTradingRepository(options =>
                                {
                                    options.ConnectionString = context.Configuration.GetConnectionString("Trader");
                                })
                                .AddTraderDashboard(options =>
                                {
                                    options.Port = 6002;
                                });

                            var templates = new[] { "ValueAveragingTemplate1", "ValueAveragingTemplate2" };

                            foreach (var template in templates)
                            {
                                var templateSection = context.Configuration.GetSection(template);

                                foreach (var symbol in templateSection.GetSection("Symbols").Get<string[]>() ?? Array.Empty<string>())
                                {
                                    services
                                        .AddValueAveragingAlgo(symbol)
                                        .ConfigureHostOptions(options =>
                                        {
                                            templateSection.Bind(options);

                                            options.Symbols.Clear();
                                            options.Symbol = symbol;
                                        })
                                        .ConfigureTypeOptions(options =>
                                        {
                                            templateSection.GetSection("Options").Bind(options);
                                        });
                                }
                            }
                        });
                })
                .RunConsoleAsync();
        }
    }
}