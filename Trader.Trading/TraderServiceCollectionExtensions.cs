using Outcompute.Trader.Trading;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Commands.Many;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.SignificantAveragingSell;
using Outcompute.Trader.Trading.Commands.TrackingBuy;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Balances;
using Outcompute.Trader.Trading.Providers.Exchange;
using Outcompute.Trader.Trading.Providers.Klines;
using Outcompute.Trader.Trading.Providers.Orders;
using Outcompute.Trader.Trading.Providers.Savings;
using Outcompute.Trader.Trading.Providers.Tickers;
using Outcompute.Trader.Trading.Providers.Trades;
using Outcompute.Trader.Trading.Readyness;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TraderServiceCollectionExtensions
    {
        public static IServiceCollection AddTradingServices(this IServiceCollection services)
        {
            return services

                // assorted services
                .AddWatchdogService()
                .AddSingleton<IReadynessProvider, ReadynessProvider>()
                .AddSingleton<ISignificantOrderResolver, SignificantOrderResolver>()
                .AddSingleton<IOrderSynchronizer, OrderSynchronizer>()
                .AddSingleton<ITradeSynchronizer, TradeSynchronizer>()
                .AddSingleton<IOrderCodeGenerator, OrderCodeGenerator>()
                .AddSingleton<IAlgoDependencyInfo, AlgoDependencyInfo>()
                .AddOptions<AlgoConfigurationMappingOptions>().ValidateDataAnnotations().Services
                .AddOptions<SavingsOptions>().ValidateDataAnnotations().Services

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

                // algo context
                .AddScoped<AlgoContext>()
                .AddScoped<IAlgoContext>(sp => sp.GetRequiredService<AlgoContext>())

                // exchange info provider
                .AddSingleton<IExchangeInfoProvider, ExchangeInfoProvider>()
                .AddOptions<ExchangeInfoOptions>().ValidateDataAnnotations().Services

                // commands
                .AddSingleton<IAlgoCommandExecutor<AveragingSellCommand>, AveragingSellExecutor>()
                .AddSingleton<IAlgoCommandExecutor<CancelOrderCommand>, CancelOrderExecutor>()
                .AddSingleton<IAlgoCommandExecutor<ClearOpenOrdersCommand>, ClearOpenOrdersExecutor>()

                .AddSingleton<ICreateOrderService, CreateOrderService>()
                .AddSingleton<IAlgoCommandExecutor<CreateOrderCommand>, CreateOrderExecutor>()

                .AddSingleton<IEnsureSingleOrderService, EnsureSingleOrderService>()
                .AddSingleton<IAlgoCommandExecutor<EnsureSingleOrderCommand>, EnsureSingleOrderExecutor>()

                .AddSingleton<IAlgoCommandExecutor<ManyCommand>, ManyExecutor>()

                .AddSingleton<IRedeemSavingsService, RedeemSavingsService>()
                .AddSingleton<IAlgoCommandExecutor<RedeemSavingsCommand>, RedeemSavingsExecutor>()

                .AddSingleton<ISignificantAveragingSellService, SignificantAveragingSellService>()
                .AddSingleton<IAlgoCommandExecutor<SignificantAveragingSellCommand>, SignificantAveragingSellExecutor>()

                .AddSingleton<ITrackingBuyService, TrackingBuyService>()
                .AddSingleton<IAlgoCommandExecutor<TrackingBuyCommand>, TrackingBuyExecutor>();
        }
    }
}