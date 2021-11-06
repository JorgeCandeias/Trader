using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Statistics;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Swap;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnvironmentName = Microsoft.Extensions.Hosting.EnvironmentName;
using ILogger = Microsoft.Extensions.Logging.ILogger;

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
                                })
                                .Configure<SwapPoolOptions>(options =>
                                {
                                    options.AutoAddEnabled = true;
                                    options.IsolatedAssets.Add("BTC");
                                    options.IsolatedAssets.Add("ETH");
                                    options.ExcludedAssets.Add("BNB");
                                    options.ExcludedAssets.Add("XMR");
                                })
                                .AddDiscoveryAlgo()
                                .AddAlgoType<TestAlgo, TestAlgoOptions>();

#if DEBUG
                            services
                                .AddAlgo<TestAlgo, TestAlgoOptions>("MyTestAlgo",
                                    options =>
                                    {
                                        options.DependsOn.Tickers.Add("BTCGBP");
                                        options.DependsOn.Balances.Add("BTCGBP");
                                        options.DependsOn.Klines.Add("BTCGBP", KlineInterval.Days1, 100);
                                    },
                                    options =>
                                    {
                                        options.SomeValue = "SomeValue";
                                    });
#endif
                        });
                })
                .RunConsoleAsync();
        }

        internal class TestAlgo : Algo
        {
            private readonly IOptionsMonitor<TestAlgoOptions> _options;
            private readonly ILogger _logger;
            private readonly IAlgoContext _context;
            private readonly ISystemClock _clock;

            public TestAlgo(IOptionsMonitor<TestAlgoOptions> options, ILogger<TestAlgo> logger, IAlgoContext context, ISystemClock clock)
            {
                _options = options ?? throw new ArgumentNullException(nameof(options));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            }

            public override async Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
            {
                var options = _options.Get(_context.Name);

                var end = _clock.UtcNow;
                var start = end.Subtract(TimeSpan.FromDays(100));

                var klines = await _context.GetKlineProvider().GetKlinesAsync(options.Symbol, KlineInterval.Days1, start, end, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("{Name} reports SMA7 = {Value}", nameof(TestAlgo), klines.LastSma(x => x.ClosePrice, 7));
                _logger.LogInformation("{Name} reports SMA25 = {Value}", nameof(TestAlgo), klines.LastSma(x => x.ClosePrice, 25));
                _logger.LogInformation("{Name} reports SMA99 = {Value}", nameof(TestAlgo), klines.LastSma(x => x.ClosePrice, 99));
                _logger.LogInformation("{Name} reports RSI6 = {Value}", nameof(TestAlgo), klines.LastRsi(x => x.ClosePrice, 6));
                _logger.LogInformation("{Name} reports RSI12 = {Value}", nameof(TestAlgo), klines.LastRsi(x => x.ClosePrice, 12));
                _logger.LogInformation("{Name} reports RSI24 = {Value}", nameof(TestAlgo), klines.LastRsi(x => x.ClosePrice, 24));

                return Noop();
            }
        }

        public class TestAlgoOptions
        {
            [Required]
            public string SomeValue { get; set; } = "Default";

            [Required]
            public string Symbol { get; set; } = "BTCGBP";
        }
    }
}