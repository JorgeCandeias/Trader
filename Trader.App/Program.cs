using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Hosting;
using Outcompute.Trader.Trading.Algorithms;
using Serilog;
using Serilog.Events;
using System;
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
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .Filter.ByExcluding(x => x.Properties.TryGetValue("SourceContext", out var property) && property is ScalarValue scalar && scalar.Value.Equals("System.Net.Http.HttpClient.BinanceApiClient.ClientHandler") && x.Level < LogEventLevel.Warning)
                        .Filter.ByExcluding(x => x.Properties.TryGetValue("SourceContext", out var property) && property is ScalarValue scalar && scalar.Value.Equals("System.Net.Http.HttpClient.BinanceApiClient.LogicalHandler") && x.Level < LogEventLevel.Warning)
                        .Filter.ByExcluding(x => x.Properties.TryGetValue("SourceContext", out var property) && property is ScalarValue scalar && scalar.Value.Equals("Orleans.Runtime.SiloControl") && x.Level < LogEventLevel.Warning)
                        .Filter.ByExcluding(x => x.Properties.TryGetValue("SourceContext", out var property) && property is ScalarValue scalar && scalar.Value.Equals("Orleans.Runtime.Management.ManagementGrain") && x.Level < LogEventLevel.Warning)
                        .WriteTo.Console()
                        .CreateLogger(), true);
                })
                .UseOrleans((context, orleans) =>
                {
                    orleans.UseLocalhostClustering();
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

                                // temporary brute force configuration - to refactor into dynamic dependency graph once orleans is brought in
                                options.MarketDataStreamSymbols.UnionWith(context
                                    .Configuration
                                    .GetSection("Trader:Algos")
                                    .GetChildren()
                                    .Select(x => x.GetSection("Options"))
                                    .Select(x => x["Symbol"])
                                    .Where(x => x is not null));

                                // temporary brute force configuration - to refactor into dynamic dependency graph once orleans is brought in
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

        internal class TestAlgo : IAlgo
        {
            private readonly IOptionsMonitor<TestAlgoOptions> _options;
            private readonly ILogger _logger;
            private readonly IAlgoContext _context;

            public TestAlgo(IOptionsMonitor<TestAlgoOptions> options, ILogger<TestAlgo> logger, IAlgoContext context)
            {
                _options = options ?? throw new ArgumentNullException(nameof(options));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _context = context ?? throw new ArgumentNullException(nameof(context));
            }

            public Task GoAsync(CancellationToken cancellationToken = default)
            {
                var options = _options.Get(_context.Name);

                _logger.LogInformation("My name is {Name} and my options are {@Options}", _context.Name, options);

                return Task.CompletedTask;
            }
        }

        public class TestAlgoOptions
        {
            [Required]
            public string SomeValue { get; set; } = "Default";
        }
    }
}