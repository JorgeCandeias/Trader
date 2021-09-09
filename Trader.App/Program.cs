using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;
using System.Linq;
using System.Threading.Tasks;

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
                        .WriteTo.Console()
                        .CreateLogger(), true);
                })
                .ConfigureServices((context, services) =>
                {
                    services

                        // temporary brute force configuration - to refactor into dynamic dependency graph once orleans is brought in
                        .AddUserDataStreamHost(options =>
                        {
                            options.Symbols.UnionWith(context
                                .Configuration
                                .GetSection("Trader:Algos")
                                .GetChildren()
                                .Select(x => x.GetSection("Options"))
                                .Select(x => x["Symbol"]));
                        });
                })
                .UseOrleans(orleans =>
                {
                    orleans.UseLocalhostClustering();
                })
                .UseTrader((context, trader) =>
                {
                    trader
                        .AddSqlTradingRepository(options =>
                        {
                            options.ConnectionString = context.Configuration.GetConnectionString("Trader");
                        })
                        .UseBinanceTradingService(options =>
                        {
                            context.Configuration.Bind("Binance", options);

                            // temporary brute force configuration - to refactor into dynamic dependency graph once orleans is brought in
                            options.MarketDataStreamSymbols.UnionWith(context
                                .Configuration
                                .GetSection("Trader:Algos")
                                .GetChildren()
                                .Select(x => x.GetSection("Options"))
                                .Select(x => x["Symbol"]));
                        });
                })
                .RunConsoleAsync();
        }
    }
}