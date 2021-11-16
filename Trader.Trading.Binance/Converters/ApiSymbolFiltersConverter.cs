using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters;

/// <summary>
/// Converts a source symbol filter api collection into the specialized symbol filter business model lookup.
/// </summary>
internal class ApiSymbolFiltersConverter : ITypeConverter<IEnumerable<ApiSymbolFilter>, SymbolFilters>
{
    public SymbolFilters Convert(IEnumerable<ApiSymbolFilter> source, SymbolFilters destination, ResolutionContext context)
    {
        if (source is null) return SymbolFilters.Empty;

        PriceSymbolFilter? price = null;
        PercentPriceSymbolFilter? percentPrice = null;
        LotSizeSymbolFilter? lotSize = null;
        MinNotionalSymbolFilter? minNotional = null;
        IcebergPartsSymbolFilter? icebergParts = null;
        MarketLotSizeSymbolFilter? marketLotSize = null;
        MaxNumberOfOrdersSymbolFilter? maxNumberOfOrders = null;
        MaxNumberOfAlgoOrdersSymbolFilter? maxNumberOfAlgoOrders = null;
        MaxNumberOfIcebergOrdersSymbolFilter? maxNumberOfIcebergOrders = null;
        MaxPositionSymbolFilter? maxPosition = null;

        foreach (var item in source)
        {
            var model = context.Mapper.Map<SymbolFilter>(item);

            switch (model)
            {
                case PriceSymbolFilter sf1:
                    price = sf1;
                    break;

                case PercentPriceSymbolFilter sf2:
                    percentPrice = sf2;
                    break;

                case LotSizeSymbolFilter sf3:
                    lotSize = sf3;
                    break;

                case MinNotionalSymbolFilter sf4:
                    minNotional = sf4;
                    break;

                case IcebergPartsSymbolFilter sf5:
                    icebergParts = sf5;
                    break;

                case MarketLotSizeSymbolFilter sf6:
                    marketLotSize = sf6;
                    break;

                case MaxNumberOfOrdersSymbolFilter sf7:
                    maxNumberOfOrders = sf7;
                    break;

                case MaxNumberOfAlgoOrdersSymbolFilter sf8:
                    maxNumberOfAlgoOrders = sf8;
                    break;

                case MaxNumberOfIcebergOrdersSymbolFilter sf9:
                    maxNumberOfIcebergOrders = sf9;
                    break;

                case MaxPositionSymbolFilter sf10:
                    maxPosition = sf10;
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(source), $"Cannot map source symbol filter of type '{model.GetType().FullName}'");
            }
        }

        return new SymbolFilters(
            price ?? PriceSymbolFilter.Empty,
            percentPrice ?? PercentPriceSymbolFilter.Empty,
            lotSize ?? LotSizeSymbolFilter.Empty,
            minNotional ?? MinNotionalSymbolFilter.Empty,
            icebergParts ?? IcebergPartsSymbolFilter.Empty,
            marketLotSize ?? MarketLotSizeSymbolFilter.Empty,
            maxNumberOfOrders ?? MaxNumberOfOrdersSymbolFilter.Empty,
            maxNumberOfAlgoOrders ?? MaxNumberOfAlgoOrdersSymbolFilter.Empty,
            maxNumberOfIcebergOrders ?? MaxNumberOfIcebergOrdersSymbolFilter.Empty,
            maxPosition ?? MaxPositionSymbolFilter.Empty);
    }
}