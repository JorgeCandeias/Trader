using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System;
using Outcompute.Trader.Hosting;
using Outcompute.Trader.Trading;
using Outcompute.Trader.Trading.Binance;
using Outcompute.Trader.Trading.Binance.Converters;
using Outcompute.Trader.Trading.Binance.Handlers;
using Outcompute.Trader.Trading.Binance.Signing;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BinanceTraderHostBuilderExtensions
    {
        public const string HasBinanceTradingServicesKey = "HasBinanceTradingServices";

        public static ITraderHostBuilder UseBinanceTradingService(this ITraderHostBuilder trader, Action<BinanceOptions> configure)
        {
            if (trader is null) throw new ArgumentNullException(nameof(trader));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            trader.ConfigureServices((context, services) =>
            {
                // add core services only once
                if (!context.Properties.TryGetValue(HasBinanceTradingServicesKey, out var flag))
                {
                    services

                        // add implementation
                        .AddSingleton<BinanceUsageContext>()
                        .AddSingleton<BinanceTradingService>()
                        .AddSingleton<ITradingService, BinanceTradingService>(sp => sp.GetRequiredService<BinanceTradingService>())
                        .AddSingleton<IHostedService, BinanceTradingService>(sp => sp.GetRequiredService<BinanceTradingService>())
                        .AddSingleton<BinanceApiConcurrencyHandler>()
                        .AddSingleton<BinanceApiCircuitBreakerHandler>()
                        .AddSingleton<BinanceApiSigningPreHandler>()
                        .AddSingleton<BinanceApiErrorPostHandler>()
                        .AddSingleton<BinanceApiUsagePostHandler>()
                        .AddSingleton<ISigner, Signer>()
                        .AddSingleton<IUserDataStreamClientFactory, BinanceUserDataStreamWssClientFactory>()
                        .AddSingleton<IMarketDataStreamClientFactory, BinanceMarketDataStreamWssClientFactory>()

                        // add typed http client
                        .AddHttpClient<BinanceApiClient>((p, x) =>
                        {
                            var options = p.GetRequiredService<IOptions<BinanceOptions>>().Value;

                            x.BaseAddress = options.BaseApiAddress;
                            x.Timeout = options.Timeout;
                        })
                        .AddHttpMessageHandler<BinanceApiConcurrencyHandler>()
                        .AddHttpMessageHandler<BinanceApiCircuitBreakerHandler>()
                        .AddHttpMessageHandler<BinanceApiSigningPreHandler>()
                        .AddHttpMessageHandler<BinanceApiErrorPostHandler>()
                        .AddHttpMessageHandler<BinanceApiUsagePostHandler>()
                        .Services

                        // add auto mapper
                        .AddAutoMapper(options =>
                        {
                            options.AddProfile<BinanceAutoMapperProfile>();
                        })
                        .AddSingleton<ServerTimeConverter>()
                        .AddSingleton<TimeZoneInfoConverter>()
                        .AddSingleton<DateTimeConverter>()
                        .AddSingleton<RateLimitConverter>()
                        .AddSingleton<SymbolStatusConverter>()
                        .AddSingleton<OrderTypeConverter>()
                        .AddSingleton<SymbolFilterConverter>()
                        .AddSingleton<PermissionConverter>()
                        .AddSingleton<OrderSideConverter>()
                        .AddSingleton<TimeInForceConverter>()
                        .AddSingleton<NewOrderResponseTypeConverter>()
                        .AddSingleton<OrderStatusConverter>()
                        .AddSingleton<ContingencyTypeConverter>()
                        .AddSingleton<OcoStatusConverter>()
                        .AddSingleton<OcoOrderStatusConverter>()
                        .AddSingleton<CancelAllOrdersResponseModelConverter>()
                        .AddSingleton<AccountTypeConverter>()
                        .AddSingleton<UserDataStreamMessageConverter>()
                        .AddSingleton<ExecutionTypeConverter>()
                        .AddSingleton<MarketDataStreamMessageConverter>()
                        .AddSingleton<KlineIntervalConverter>()
                        .AddSingleton<FlexibleProductRedemptionTypeConverter>()
                        .AddSingleton<FlexibleProductStatusConverter>()
                        .AddSingleton<FlexibleProductFeaturedConverter>()
                        .AddSingleton(typeof(ImmutableListConverter<,>)); // todo: move this to the shared model converters

                    // add object pool
                    services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
                    services.TryAddSingleton(sp => sp.GetRequiredService<ObjectPoolProvider>().CreateStringBuilderPool());
                }

                // always bind to the options delegate
                services
                    .AddOptions<BinanceOptions>()
                    .Configure(configure)
                    .ValidateDataAnnotations();
            });

            return trader;
        }
    }
}