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
                            options.Symbols.Add("BNBGBP");
                            options.Symbols.Add("XMRBNB");
                            options.Symbols.Add("COCOSBNB");
                            options.Symbols.Add("ZECBNB");
                            options.Symbols.Add("DASHBNB");
                            options.Symbols.Add("XMRBTC");
                            options.Symbols.Add("BTCGBP");
                            options.Symbols.Add("ETHGBP");
                            options.Symbols.Add("ADAGBP");
                            options.Symbols.Add("XRPGBP");
                            options.Symbols.Add("LINKGBP");
                            options.Symbols.Add("DOGEGBP");
                            options.Symbols.Add("SXPGBP");
                            options.Symbols.Add("DOTGBP");
                            options.Symbols.Add("CHZGBP");
                            options.Symbols.Add("LTCGBP");
                            options.Symbols.Add("ENJGBP");
                            options.Symbols.Add("VETGBP");
                            options.Symbols.Add("CAKEGBP");
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
                        .AddAccumulatorAlgorithm("BNBGBP", options => context.Configuration.Bind("Trading:Algorithms:Accumulator:BNBGBP", options))
                        .AddAccumulatorAlgorithm("BTCGBP", options => context.Configuration.Bind("Trading:Algorithms:Accumulator:BTCGBP", options))
                        .AddStepAlgorithm("XMRBNB", options => context.Configuration.Bind("Trading:Algorithms:Step:XMRBNB", options))
                        .AddStepAlgorithm("COCOSBNB", options => context.Configuration.Bind("Trading:Algorithms:Step:COCOSBNB", options))
                        .AddStepAlgorithm("ZECBNB", options => context.Configuration.Bind("Trading:Algorithms:Step:ZECBNB", options))
                        .AddStepAlgorithm("DASHBNB", options => context.Configuration.Bind("Trading:Algorithms:Step:DASHBNB", options))
                        .AddStepAlgorithm("XMRBTC", options => context.Configuration.Bind("Trading:Algorithms:Step:XMRBTC", options))
                        .AddStepAlgorithm("ETHGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:ETHGBP", options))
                        .AddStepAlgorithm("ADAGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:ADAGBP", options))
                        .AddStepAlgorithm("XRPGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:XRPGBP", options))
                        .AddStepAlgorithm("LINKGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:LINKGBP", options))
                        .AddStepAlgorithm("DOGEGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:DOGEGBP", options))
                        .AddStepAlgorithm("SXPGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:SXPGBP", options))
                        .AddStepAlgorithm("DOTGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:DOTGBP", options))
                        .AddStepAlgorithm("CHZGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:CHZGBP", options))
                        .AddStepAlgorithm("LTCGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:LTCGBP", options))
                        .AddStepAlgorithm("ENJGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:ENJGBP", options))
                        .AddStepAlgorithm("VETGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:VETGBP", options))
                        .AddStepAlgorithm("CAKEGBP", options => context.Configuration.Bind("Trading:Algorithms:Step:CAKEGBP", options));
                })
                .RunConsoleAsync();
        }
    }
}