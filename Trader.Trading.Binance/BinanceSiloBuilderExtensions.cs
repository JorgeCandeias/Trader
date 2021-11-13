using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading;
using Outcompute.Trader.Trading.Binance;
using Outcompute.Trader.Trading.Binance.Converters;
using Outcompute.Trader.Trading.Binance.Handlers;
using Outcompute.Trader.Trading.Binance.Providers.MarketData;
using Outcompute.Trader.Trading.Binance.Providers.UserData;
using Outcompute.Trader.Trading.Binance.Signing;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Orleans.Hosting
{
    [ExcludeFromCodeCoverage]
    public static class BinanceSiloBuilderExtensions
    {
        public static ISiloBuilder AddBinanceTradingService(this ISiloBuilder builder, Action<BinanceOptions> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            // add the kitchen sink
            builder
                .ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(BinanceSiloBuilderExtensions).Assembly).WithReferences())
                .ConfigureServices(services =>
                {
                    services

                        // add options
                        .AddOptions<BinanceOptions>()
                        .Configure(configure)
                        .ValidateDataAnnotations()
                        .Services

                        // add implementation
                        .AddSingleton<BinanceUsageContext>()
                        .AddSingleton<BinanceTradingService>()
                        .AddSingleton<ITradingService, BinanceTradingService>(sp => sp.GetRequiredService<BinanceTradingService>())
                        .AddSingleton<IHostedService, BinanceTradingService>(sp => sp.GetRequiredService<BinanceTradingService>())
                        .AddSingleton<BinanceTradingServiceWithBackoff>()
                        .AddSingleton<BinanceApiConcurrencyHandler>()
                        .AddSingleton<BinanceApiSigningPreHandler>()
                        .AddSingleton<BinanceApiErrorPostHandler>()
                        .AddSingleton<BinanceApiUsageHandler>()
                        .AddSingleton<ISigner, Signer>()
                        .AddSingleton<IUserDataStreamClientFactory, BinanceUserDataStreamWssClientFactory>()
                        .AddSingleton<IMarketDataStreamClientFactory, BinanceMarketDataStreamWssClientFactory>()
                        .AddSingleton<ITickerSynchronizer, TickerSynchronizer>()
                        .AddSingleton<IKlineSynchronizer, KlineSynchronizer>()
                        .AddSingleton<IMarketDataStreamer, MarketDataStreamer>()

                        // add typed http client
                        .AddHttpClient<BinanceApiClient>((p, x) =>
                        {
                            var options = p.GetRequiredService<IOptions<BinanceOptions>>().Value;

                            x.BaseAddress = options.BaseApiAddress;
                            x.Timeout = options.Timeout;
                        })
                        .AddHttpMessageHandler<BinanceApiConcurrencyHandler>()
                        .AddHttpMessageHandler<BinanceApiSigningPreHandler>()
                        .AddHttpMessageHandler<BinanceApiErrorPostHandler>()
                        .AddHttpMessageHandler<BinanceApiUsageHandler>()
                        .Services

                        // add auto mapper
                        .AddAutoMapper(options =>
                        {
                            options.AddProfile<BinanceAutoMapperProfile>();
                        })
                        .AddSingleton<ApiServerTimeConverter>()
                        .AddSingleton<TimeZoneInfoConverter>()
                        .AddSingleton<DateTimeConverter>()
                        .AddSingleton<ApiRateLimiterConverter>()
                        .AddSingleton<SymbolStatusConverter>()
                        .AddSingleton<OrderTypeConverter>()
                        .AddSingleton<ApiSymbolFilterConverter>()
                        .AddSingleton<ApiSymbolFiltersConverter>()
                        .AddSingleton<PermissionConverter>()
                        .AddSingleton<OrderSideConverter>()
                        .AddSingleton<TimeInForceConverter>()
                        .AddSingleton<NewOrderResponseTypeConverter>()
                        .AddSingleton<OrderStatusConverter>()
                        .AddSingleton<ContingencyTypeConverter>()
                        .AddSingleton<OcoStatusConverter>()
                        .AddSingleton<OcoOrderStatusConverter>()
                        .AddSingleton<CancelAllOrdersResponseConverter>()
                        .AddSingleton<AccountTypeConverter>()
                        .AddSingleton<UserDataStreamMessageConverter>()
                        .AddSingleton<ExecutionTypeConverter>()
                        .AddSingleton<MarketDataStreamMessageConverter>()
                        .AddSingleton<KlineIntervalConverter>()
                        .AddSingleton<SavingsRedemptionTypeConverter>()
                        .AddSingleton<SavingsStatusConverter>()
                        .AddSingleton<SavingsFeaturedConverter>()
                        .AddSingleton<SwapPoolLiquidityTypeConverter>()

                        // add watchdog entries
                        .AddWatchdogEntry((sp, ct) => sp.GetRequiredService<IGrainFactory>().GetBinanceMarketDataGrain().PingAsync())
                        .AddWatchdogEntry((sp, ct) => sp.GetRequiredService<IGrainFactory>().GetBinanceUserDataGrain().PingAsync())

                        // add readyness entries
                        .AddReadynessEntry(sp => sp.GetRequiredService<IGrainFactory>().GetBinanceUserDataReadynessGrain().IsReadyAsync())
                        .AddReadynessEntry(sp => sp.GetRequiredService<IGrainFactory>().GetBinanceMarketDataReadynessGrain().IsReadyAsync());

                    // add object pool
                    services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
                    services.TryAddSingleton(sp => sp.GetRequiredService<ObjectPoolProvider>().CreateStringBuilderPool());
                });

            return builder;
        }
    }
}