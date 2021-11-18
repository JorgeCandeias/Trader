using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Orleans;
using Outcompute.Trader.Trading;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Algorithms.Positions;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using Outcompute.Trader.Trading.Commands.CancelOpenOrders;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Commands.MarketBuy;
using Outcompute.Trader.Trading.Commands.MarketSell;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;
using Outcompute.Trader.Trading.Commands.Sequence;
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

namespace Microsoft.Extensions.DependencyInjection;

public static class TraderServiceCollectionExtensions
{
    public static IServiceCollection AddTradingServices(this IServiceCollection services)
    {
        // add or reuse object pools
        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        services.TryAddSingleton(sp => sp.GetRequiredService<ObjectPoolProvider>().CreateStringBuilderPool());

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
            .AddSingleton<IAlgoStatisticsPublisher, AlgoStatisticsPublisher>()
            .AddSingleton<IAlgoDependencyResolver, AlgoDependencyResolver>()
            .AddOptions<AlgoConfigurationMappingOptions>().ValidateDataAnnotations().Services
            .AddOptions<SavingsOptions>().ValidateDataAnnotations().Services
            .ConfigureOptions<AlgoDependencyOptionsConfigurator>()
            .ConfigureOptions<TraderOptionsConfigurator>().AddOptions<TraderOptions>().ValidateDataAnnotations().Services

            // tag generator
            .AddSingleton<ITagGenerator, TagGenerator>()
            .AddOptions<TagGenerator>().ValidateDataAnnotations().Services

            // algo options
            .AddOptions<AlgoOptions>().ValidateDataAnnotations().Services
            .ConfigureOptions<AlgoOptionsConfigurator>()

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

            // algo factory resolver
            .AddScoped<IAlgoFactoryResolver, AlgoFactoryResolver>()

            // algo scope context
            .AddScoped(sp => sp.GetRequiredService<IAlgoContextLocal>().Context)
            .AddScoped<IAlgoContextLocal, AlgoContextLocal>()
            .AddScoped<IAlgoContextFactory, AlgoContextFactory>()

            // algo context configurators in order
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextTickTimeConfigurator>()
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextExchangeInfoConfigurator>()
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextSymbolConfigurator>()
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextTickerConfigurator>()
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextSpotBalanceConfigurator>()
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextSavingsConfigurator>()
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextSwapPoolBalanceConfigurator>()
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextOrdersConfigurator>()
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextTradesConfigurator>()
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextKlinesConfigurator>()
            .AddSingleton<IAlgoContextConfigurator<AlgoContext>, AlgoContextAutoPositionsConfigurator>()

            // exchange info provider
            .AddSingleton<ExchangeInfoProvider>()
            .AddSingleton<IHostedService>(sp => sp.GetRequiredService<ExchangeInfoProvider>())
            .AddSingleton<IExchangeInfoProvider>(sp => sp.GetRequiredService<ExchangeInfoProvider>())
            .AddOptions<ExchangeInfoOptions>().ValidateDataAnnotations().Services

            // commands
            .AddSingleton<IAlgoCommandExecutor<AveragingSellCommand>, AveragingSellExecutor>()
            .AddSingleton<IAlgoCommandExecutor<CancelOrderCommand>, CancelOrderExecutor>()
            .AddSingleton<IAlgoCommandExecutor<CancelOpenOrdersCommand>, CancelOpenOrdersExecutor>()
            .AddSingleton<IAlgoCommandExecutor<CreateOrderCommand>, CreateOrderExecutor>()
            .AddSingleton<IAlgoCommandExecutor<EnsureSingleOrderCommand>, EnsureSingleOrderExecutor>()
            .AddSingleton<IAlgoCommandExecutor<SequenceCommand>, SequenceExecutor>()
            .AddSingleton<IAlgoCommandExecutor<RedeemSavingsCommand, RedeemSavingsEvent>, RedeemSavingsExecutor>()
            .AddSingleton<IAlgoCommandExecutor<SignificantAveragingSellCommand>, SignificantAveragingSellExecutor>()
            .AddSingleton<IAlgoCommandExecutor<TrackingBuyCommand>, TrackingBuyExecutor>()
            .AddSingleton<IAlgoCommandExecutor<RedeemSwapPoolCommand, RedeemSwapPoolEvent>, RedeemSwapPoolExecutor>()
            .AddSingleton<IAlgoCommandExecutor<MarketSellCommand>, MarketSellCommandExecutor>()
            .AddSingleton<IAlgoCommandExecutor<MarketBuyCommand>, MarketBuyCommandExecutor>()
            .AddSingleton<IAlgoCommandExecutor<CancelOpenOrdersCommand>, CancelOpenOrdersExecutor>()

            // builtin algos
            .AddAccumulatorAlgoType()
            .AddArbitrageAlgoType()
            .AddValueAveragingAlgoType()
            .AddDiscoveryAlgoType()
            .AddPortfolioAlgoType();
    }
}