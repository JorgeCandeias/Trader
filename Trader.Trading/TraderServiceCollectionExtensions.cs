using Outcompute.Trader.Trading;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Operations;
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

                // blocks
                .AddAveragingSellServices()
                .AddSingleton<ICreateOrderOperation, CreateOrderOperation>()
                .AddSingleton<IEnsureSingleOrderOperation, EnsureSingleOrderOperation>()
                .AddSingleton<ICancelOrderOperation, CancelOrderOperation>()
                .AddSingleton<IClearOpenOrdersOperation, ClearOpenOrdersOperation>()
                .AddSingleton<IGetOpenOrdersOperation, GetOpenOrdersOperation>()
                .AddSingleton<IRedeemSavingsOperation, RedeemSavingsOperation>()
                .AddSingleton<ISignificantAveragingSellOperation, SignificantAveragingSellOperation>()
                .AddSingleton<ITrackingBuyOperation, TrackingBuyOperation>();
        }
    }
}