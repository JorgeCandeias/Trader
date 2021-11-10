using Orleans;
using Outcompute.Trader.Trading;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Commands.Many;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;
using Outcompute.Trader.Trading.Commands.SignificantAveragingSell;
using Outcompute.Trader.Trading.Commands.TrackingBuy;
using Outcompute.Trader.Trading.Configuration;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Balances;
using Outcompute.Trader.Trading.Providers.Exchange;
using Outcompute.Trader.Trading.Providers.Klines;
using Outcompute.Trader.Trading.Providers.Orders;
using Outcompute.Trader.Trading.Providers.Savings;
using Outcompute.Trader.Trading.Providers.Swap;
using Outcompute.Trader.Trading.Providers.Tickers;
using Outcompute.Trader.Trading.Providers.Trades;
using Outcompute.Trader.Trading.Readyness;
using Outcompute.Trader.Trading.Watchdog;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TraderServiceCollectionExtensions
    {
        public static IServiceCollection AddTradingServices(this IServiceCollection services)
        {
            return services

                // add watchdog entires
                .AddWatchdogEntry((sp, ct) => sp.GetRequiredService<IGrainFactory>().GetSwapPoolGrain().AsReference<IWatchdogGrainExtension>().PingAsync())
                .AddWatchdogEntry((sp, ct) => sp.GetRequiredService<IGrainFactory>().GetAlgoManagerGrain().AsReference<IWatchdogGrainExtension>().PingAsync())
                .AddWatchdogEntry((sp, ct) => sp.GetRequiredService<IGrainFactory>().GetSavingsGrain().AsReference<IWatchdogGrainExtension>().PingAsync())

                // add readyness entries
                .AddReadynessEntry((sp, ct) => sp.GetRequiredService<IGrainFactory>().GetSavingsGrain().IsReadyAsync())
                .AddReadynessEntry((sp, ct) => sp.GetRequiredService<IGrainFactory>().GetSwapPoolGrain().IsReadyAsync())

                // assorted services
                .AddWatchdogService()
                .AddSingleton<IReadynessProvider, ReadynessProvider>()
                .AddSingleton<IAutoPositionResolver, AutoPositionResolver>()
                .AddSingleton<IOrderSynchronizer, OrderSynchronizer>()
                .AddSingleton<ITradeSynchronizer, TradeSynchronizer>()
                .AddSingleton<IOrderCodeGenerator, OrderCodeGenerator>()
                .AddSingleton<IAlgoContextHydrator, AlgoContextHydrator>()
                .AddSingleton<IAlgoStatisticsPublisher, AlgoStatisticsPublisher>()
                .AddSingleton<IAlgoDependencyResolver, AlgoDependencyResolver>()
                .AddOptions<AlgoConfigurationMappingOptions>().ValidateDataAnnotations().Services
                .AddOptions<SavingsOptions>().ValidateDataAnnotations().Services
                .ConfigureOptions<AlgoDependencyOptionsConfigurator>()
                .ConfigureOptions<TraderOptionsConfigurator>().AddOptions<TraderOptions>().ValidateDataAnnotations().Services

                // kline provider
                .AddSingleton<IKlineProvider, KlineProvider>()
                .AddOptions<KlineProviderOptions>().ValidateDataAnnotations().Services

                // order provider
                .AddSingleton<IOrderProvider, OrderProvider>()
                .AddOptions<OrderProviderOptions>().ValidateDataAnnotations().Services

                // ticker provider
                .AddSingleton<ITickerProvider, TickerProvider>()

                // savings provider
                .AddSingleton<ISavingsProvider, SavingsProvider>()
                .AddOptions<SavingsProviderOptions>().ValidateDataAnnotations().Services

                // balance provider
                .AddSingleton<IBalanceProvider, BalanceProvider>()

                // trade provider
                .AddSingleton<ITradeProvider, TradeProvider>()
                .AddOptions<TradeProviderOptions>().ValidateDataAnnotations().Services

                // swap pool provider
                .AddSingleton<ISwapPoolProvider, SwapPoolProvider>()
                .AddOptions<SwapPoolOptions>().ValidateDataAnnotations().Services
                .ConfigureOptions<SwapPoolOptionsConfigurator>()

                // algo context
                .AddScoped<AlgoContext>()
                .AddScoped<IAlgoContext>(sp => sp.GetRequiredService<AlgoContext>())

                // algo context configurators in order
                .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextSymbolConfigurator>()
                .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextPositionsConfigurator>()
                .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextTickerConfigurator>()
                .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextSpotBalanceConfigurator>()
                .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextSavingsConfigurator>()
                .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextSwapPoolBalanceConfigurator>()

                // exchange info provider
                .AddSingleton<IExchangeInfoProvider, ExchangeInfoProvider>()
                .AddOptions<ExchangeInfoOptions>().ValidateDataAnnotations().Services

                // commands
                .AddSingleton<IAlgoCommandExecutor<AveragingSellCommand>, AveragingSellExecutor>()
                .AddSingleton<IAlgoCommandExecutor<CancelOrderCommand>, CancelOrderExecutor>()
                .AddSingleton<IAlgoCommandExecutor<ClearOpenOrdersCommand>, ClearOpenOrdersExecutor>()
                .AddSingleton<IAlgoCommandExecutor<CreateOrderCommand>, CreateOrderExecutor>()
                .AddSingleton<IAlgoCommandExecutor<EnsureSingleOrderCommand>, EnsureSingleOrderExecutor>()
                .AddSingleton<IAlgoCommandExecutor<ManyCommand>, ManyExecutor>()
                .AddSingleton<IAlgoCommandExecutor<RedeemSavingsCommand, RedeemSavingsEvent>, RedeemSavingsExecutor>()
                .AddSingleton<IAlgoCommandExecutor<SignificantAveragingSellCommand>, SignificantAveragingSellExecutor>()
                .AddSingleton<IAlgoCommandExecutor<TrackingBuyCommand>, TrackingBuyExecutor>()
                .AddSingleton<IAlgoCommandExecutor<RedeemSwapPoolCommand, RedeemSwapPoolEvent>, RedeemSwapPoolExecutor>()

                // builtin algos
                .AddValueAveragingAlgoType()
                .AddPennyAccumulatorAlgo()
                .AddDiscoveryAlgoType();
        }
    }
}