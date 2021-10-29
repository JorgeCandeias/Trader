using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Converters;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    public class SymbolFiltersConverterTests
    {
        [Fact]
        public void Converts()
        {
            // arrange
            var provider = new ServiceCollection()
                .AddSingleton<SymbolFilterConverter>()
                .AddSingleton<SymbolFiltersConverter>()
                .AddAutoMapper(x =>
                {
                    x.AddProfile<BinanceAutoMapperProfile>();
                })
                .BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();
            var source = new[]
            {
                SymbolFilterModel.Empty with { FilterType = "PRICE_FILTER", MinPrice = 1, MaxPrice = 2, TickSize = 3 },
                SymbolFilterModel.Empty with { FilterType = "PERCENT_PRICE", MultiplierUp = 1, MultiplierDown = 2, AvgPriceMins = 3 },
                SymbolFilterModel.Empty with { FilterType = "LOT_SIZE", MinQty = 1, MaxQty = 2, StepSize = 3 },
                SymbolFilterModel.Empty with { FilterType = "MIN_NOTIONAL", MinNotional = 1, ApplyToMarket = false, AvgPriceMins = 2 },
                SymbolFilterModel.Empty with { FilterType = "ICEBERG_PARTS", Limit = 1 },
                SymbolFilterModel.Empty with { FilterType = "MARKET_LOT_SIZE", MinQty = 1, MaxQty = 2, StepSize = 3 },
                SymbolFilterModel.Empty with { FilterType = "MAX_NUM_ORDERS", MaxNumOrders = 1 },
                SymbolFilterModel.Empty with { FilterType = "MAX_NUM_ALGO_ORDERS", MaxNumAlgoOrders = 1 },
                SymbolFilterModel.Empty with { FilterType = "MAX_NUM_ICEBERG_ORDERS", MaxNumIcebergOrders = 1 },
                SymbolFilterModel.Empty with { FilterType = "MAX_POSITION", MaxPosition = 1 }
            };

            // act
            var result = mapper.Map<SymbolFilters>(source);

            // assert
            Assert.Equal(source[0].MinPrice, result.Price.MinPrice);
            Assert.Equal(source[0].MaxPrice, result.Price.MaxPrice);
            Assert.Equal(source[0].TickSize, result.Price.TickSize);
            Assert.Equal(source[1].MultiplierUp, result.PercentPrice.MultiplierUp);
            Assert.Equal(source[1].MultiplierDown, result.PercentPrice.MultiplierDown);
            Assert.Equal(source[1].AvgPriceMins, result.PercentPrice.AvgPriceMins);
            Assert.Equal(source[2].MinQty, result.LotSize.MinQuantity);
            Assert.Equal(source[2].MaxQty, result.LotSize.MaxQuantity);
            Assert.Equal(source[2].StepSize, result.LotSize.StepSize);
            Assert.Equal(source[3].MinNotional, result.MinNotional.MinNotional);
            Assert.Equal(source[3].ApplyToMarket, result.MinNotional.ApplyToMarket);
            Assert.Equal(source[3].AvgPriceMins, result.MinNotional.AvgPriceMins);
            Assert.Equal(source[4].Limit, result.IcebergParts.Limit);
            Assert.Equal(source[5].MinQty, result.MarketLotSize.MinQuantity);
            Assert.Equal(source[5].MaxQty, result.MarketLotSize.MaxQuantity);
            Assert.Equal(source[5].StepSize, result.MarketLotSize.StepSize);
            Assert.Equal(source[6].MaxNumOrders, result.MaxNumberOfOrders.MaxNumberOfOrders);
            Assert.Equal(source[7].MaxNumAlgoOrders, result.MaxNumberOfAlgoOrders.MaxNumberOfAlgoOrders);
            Assert.Equal(source[8].MaxNumIcebergOrders, result.MaxNumberOfIcebergOrders.MaxNumberOfIcebergOrders);
            Assert.Equal(source[9].MaxPosition, result.MaxPositions.MaxPosition);
        }
    }
}