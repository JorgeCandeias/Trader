using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Threading.Tasks;

namespace Trader.App
{
    internal class Program
    {
        private static Task Main()
        {
            var temp = Path.GetTempPath();
            var db = Path.Combine(temp, "trader.db");

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
                        .AddBinanceTradingService(options => context.Configuration.Bind("Api", options))
                        .AddTradingHost()
                        .AddSystemClock()
                        .AddSafeTimerFactory()
                        .AddSqliteRepository(options =>
                        {
                            options.ConnectionString = $"Data Source={db}";
                        });

                    services
                        .AddAlgorithmResolvers()
                        .AddStepAlgorithm("BNBGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:BNBGBP", options))
                        .AddStepAlgorithm("BTCGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:BTCGBP", options))
                        .AddStepAlgorithm("ETHGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:ETHGBP", options))
                        .AddStepAlgorithm("ADAGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:ADAGBP", options))
                        .AddStepAlgorithm("XRPGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:XRPGBP", options))
                        .AddStepAlgorithm("LINKGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:LINKGBP", options))
                        .AddStepAlgorithm("DOGEGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:DOGEGBP", options))
                        .AddStepAlgorithm("SXPGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:SXPGBP", options))
                        .AddStepAlgorithm("DOTGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:DOTGBP", options))
                        .AddStepAlgorithm("CHZGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:CHZGBP", options))
                        .AddStepAlgorithm("LTCGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:LTCGBP", options))
                        .AddStepAlgorithm("ENJGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:ENJGBP", options));
                })
                .RunConsoleAsync();
        }
    }
}