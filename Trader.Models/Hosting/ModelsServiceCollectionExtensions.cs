using Trader.Models;
using Trader.Models.Collections;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ModelsServiceCollectionExtensions
    {
        /// <summary>
        /// Adds shared business-to-business model type converter services for the specialized collections.
        /// </summary>
        public static IServiceCollection AddModelServices(this IServiceCollection services)
        {
            return services
                .AddSingleton(typeof(ImmutableSortedOrderSetConverter<>))
                .AddSingleton(typeof(ImmutableSortedTradeSetConverter<>))
                .AddAutoMapper(options =>
                {
                    options.AddProfile<ImmutableSortedOrderSetProfile>();
                    options.AddProfile<ImmutableSortedTradeSetProfile>();

                    options.CreateMap<Ticker, MiniTicker>()
                        .ForCtorParam(nameof(MiniTicker.EventTime), x => x.MapFrom(y => y.CloseTime))
                        .ForCtorParam(nameof(MiniTicker.ClosePrice), x => x.MapFrom(y => y.LastPrice))
                        .ForCtorParam(nameof(MiniTicker.AssetVolume), x => x.MapFrom(y => y.Volume));

                    options.CreateMap<ExecutionReportUserDataStreamMessage, AccountTrade>()
                        .ForCtorParam(nameof(AccountTrade.Id), x => x.MapFrom(y => y.TradeId))
                        .ForCtorParam(nameof(AccountTrade.Price), x => x.MapFrom(y => y.LastExecutedPrice))
                        .ForCtorParam(nameof(AccountTrade.Quantity), x => x.MapFrom(y => y.LastExecutedQuantity))
                        .ForCtorParam(nameof(AccountTrade.QuoteQuantity), x => x.MapFrom(y => y.LastQuoteAssetTransactedQuantity))
                        .ForCtorParam(nameof(AccountTrade.Commission), x => x.MapFrom(y => y.CommissionAmount))
                        .ForCtorParam(nameof(AccountTrade.Time), x => x.MapFrom(y => y.TransactionTime))
                        .ForCtorParam(nameof(AccountTrade.IsBuyer), x => x.MapFrom(y => y.OrderSide == OrderSide.Buy))
                        .ForCtorParam(nameof(AccountTrade.IsMaker), x => x.MapFrom(y => y.IsMakerOrder))
                        .ForCtorParam(nameof(AccountTrade.IsBestMatch), x => x.MapFrom(_ => true));
                });
        }
    }
}