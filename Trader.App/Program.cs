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
                            options.Symbols.Add("BTCGBP");
                            options.Symbols.Add("BNBBTC");
                            options.Symbols.Add("XMRBNB");
                            options.Symbols.Add("XMRBTC");
                            options.Symbols.Add("COCOSBNB");
                            options.Symbols.Add("ZECBTC");
                            options.Symbols.Add("DASHBNB");
                            options.Symbols.Add("DASHBTC");
                            options.Symbols.Add("ETHBTC");
                            options.Symbols.Add("ADAGBP");
                            options.Symbols.Add("ADABTC");
                            options.Symbols.Add("XRPGBP");
                            options.Symbols.Add("XRPBTC");
                            options.Symbols.Add("LINKGBP");
                            options.Symbols.Add("LINKBTC");
                            options.Symbols.Add("DOGEGBP");
                            options.Symbols.Add("DOGEBTC");
                            options.Symbols.Add("SXPBTC");
                            options.Symbols.Add("DOTGBP");
                            options.Symbols.Add("DOTBTC");
                            options.Symbols.Add("CHZGBP");
                            options.Symbols.Add("CHZBTC");
                            options.Symbols.Add("LTCGBP");
                            options.Symbols.Add("LTCBTC");
                            options.Symbols.Add("ENJGBP");
                            options.Symbols.Add("ENJBTC");
                            options.Symbols.Add("VETGBP");
                            options.Symbols.Add("VETBTC");
                            options.Symbols.Add("CAKEGBP");
                            options.Symbols.Add("CAKEBTC");
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
                        .AddStepAlgorithm("XMRBNB", options => context.Configuration.Bind("Trading:Algorithms:Step:XMRBNB", options))
                        .AddStepAlgorithm("XMRBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:XMRBTC", options))
                        .AddStepAlgorithm("COCOSBNB", options => context.Configuration.Bind("Trading:Algorithms:Step:COCOSBNB", options))
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
                        .AddStepAlgorithm("CAKEBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:CAKEBTC", options));
                })
                .RunConsoleAsync();
        }
    }
}