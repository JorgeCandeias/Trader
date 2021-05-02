using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
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
                        .AddUserDataStreamHost(options =>
                        {
                            options.Symbols.UnionWith(new[]
                            {
                                "BTCGBP",
                                "BNBBTC",
                                "XMRBNB",
                                "XMRBTC",
                                "ZECBTC",
                                "DASHBNB",
                                "DASHBTC",
                                "ETHBTC",
                                "ADAGBP",
                                "ADABTC",
                                "XRPGBP",
                                "XRPBTC",
                                "LINKGBP",
                                "LINKBTC",
                                "DOGEGBP",
                                "DOGEBTC",
                                "SXPBTC",
                                "DOTGBP",
                                "DOTBTC",
                                "CHZGBP",
                                "CHZBTC",
                                "LTCGBP",
                                "LTCBTC",
                                "ENJGBP",
                                "ENJBTC",
                                "VETGBP",
                                "VETBTC",
                                "CAKEGBP",
                                "CAKEBTC",
                                "RIFBTC",
                                "BCHBTC",
                                "XLMBTC"
                            });
                        })
                        .AddTradingHost()
                        .AddSystemClock()
                        .AddSafeTimerFactory()
                        .AddSqlTraderRepository(options =>
                        {
                            options.ConnectionString = context.Configuration.GetConnectionString("Trader");
                        })
                        .AddBase62NumberSerializer();

                    services
                        .AddAccumulatorAlgorithm("BTCGBP", options => context.Configuration.Bind("Trading:Algorithms:Accumulator:BTCGBP", options))
                        .AddAccumulatorAlgorithm("BNBBTC", options => context.Configuration.Bind("Trading:Algorithms:Accumulator:BNBBTC", options))
                        .AddAccumulatorAlgorithm("XMRBTC", options => context.Configuration.Bind("Trading:Algorithms:Accumulator:XMRBTC", options))
                        .AddStepAlgorithm("XMRBNB", options => context.Configuration.Bind("Trading:Algorithms:Step:XMRBNB", options))
                        .AddStepAlgorithm("ZECBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:ZECBTC", options))
                        .AddStepAlgorithm("DASHBNB", options => context.Configuration.Bind("Trading:Algorithms:Step:DASHBNB", options))
                        .AddStepAlgorithm("DASHBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:DASHBTC", options))
                        .AddStepAlgorithm("ETHBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:ETHBTC", options))
                        .AddStepAlgorithm("ADAGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:ADAGBP", options))
                        .AddStepAlgorithm("ADABTC", options => context.Configuration.Bind("Trading:Algorithms:Step:ADABTC", options))
                        .AddStepAlgorithm("XRPGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:XRPGBP", options))
                        .AddStepAlgorithm("XRPBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:XRPBTC", options))
                        .AddStepAlgorithm("LINKGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:LINKGBP", options))
                        .AddStepAlgorithm("LINKBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:LINKBTC", options))
                        .AddStepAlgorithm("DOGEGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:DOGEGBP", options))
                        .AddStepAlgorithm("DOGEBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:DOGEBTC", options))
                        .AddStepAlgorithm("SXPBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:SXPBTC", options))
                        .AddStepAlgorithm("DOTGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:DOTGBP", options))
                        .AddStepAlgorithm("DOTBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:DOTBTC", options))
                        .AddStepAlgorithm("CHZGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:CHZGBP", options))
                        .AddStepAlgorithm("CHZBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:CHZBTC", options))
                        .AddStepAlgorithm("LTCGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:LTCGBP", options))
                        .AddStepAlgorithm("LTCBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:LTCBTC", options))
                        .AddStepAlgorithm("ENJGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:ENJGBP", options))
                        .AddStepAlgorithm("ENJBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:ENJBTC", options))
                        .AddStepAlgorithm("VETGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:VETGBP", options))
                        .AddStepAlgorithm("VETBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:VETBTC", options))
                        .AddStepAlgorithm("CAKEGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:CAKEGBP", options))
                        .AddStepAlgorithm("CAKEBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:CAKEBTC", options))
                        .AddStepAlgorithm("RIFBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:RIFBTC", options))
                        .AddStepAlgorithm("BCHBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:BCHBTC", options))
                        .AddStepAlgorithm("XLMBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:XLMBTC", options));
                })
                .RunConsoleAsync();
        }
    }
}