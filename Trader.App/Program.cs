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
using Outcompute.Trader.Trading.Providers;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Outcompute.Trader.App
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
                    config.AddEnvironmentVariables("Trader");
                    config.AddJsonFile("appsettings.local.json", true);
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
                    orleans.AddAdoNetGrainStorage("PubSubStore", options =>
                    {
                        options.Invariant = "Microsoft.Data.SqlClient";
                        options.ConnectionString = context.Configuration.GetConnectionString("Trader");
                    });
                    orleans.UseDashboard(options =>
                    {
                        options.Port = 6001;
                    });

                    orleans.UseTrader(trader =>
                    {
                        trader
                            .AddBinanceTradingService(options =>
                            {
                                context.Configuration.Bind("Binance", options);

                                options.UserDataStreamSymbols.UnionWith(context
                                    .Configuration
                                    .GetSection("Trader:Algos")
                                    .GetChildren()
                                    .Select(x => x.GetSection("Options"))
                                    .Select(x => x["Symbol"])
                                    .Where(x => x is not null));
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
                                    .AddAlgoType<TestAlgo, TestAlgoOptions>("Test");
                            });
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

            public override async ValueTask GoAsync(CancellationToken cancellationToken = default)
            {
                var options = _options.Get(_context.Name);

                var end = _clock.UtcNow;
                var start = end.Subtract(TimeSpan.FromDays(100));

                var klines = await _context.GetKlineProvider().GetKlinesAsync(options.Symbol, KlineInterval.Days1, start, end, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("{Name} reports SMA7 = {Value}", nameof(TestAlgo), klines.LastSimpleMovingAverage(x => x.ClosePrice, 7));
                _logger.LogInformation("{Name} reports SMA25 = {Value}", nameof(TestAlgo), klines.LastSimpleMovingAverage(x => x.ClosePrice, 25));
                _logger.LogInformation("{Name} reports SMA99 = {Value}", nameof(TestAlgo), klines.LastSimpleMovingAverage(x => x.ClosePrice, 99));
                _logger.LogInformation("{Name} reports RSI6 = {Value}", nameof(TestAlgo), klines.RelativeStrengthIndex(x => x.ClosePrice, 6));
                _logger.LogInformation("{Name} reports RSI12 = {Value}", nameof(TestAlgo), klines.RelativeStrengthIndex(x => x.ClosePrice, 12));
                _logger.LogInformation("{Name} reports RSI24 = {Value}", nameof(TestAlgo), klines.RelativeStrengthIndex(x => x.ClosePrice, 24));
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