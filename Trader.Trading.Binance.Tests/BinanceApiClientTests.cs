using AutoMapper;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;
using System.Globalization;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    public class BinanceApiClientTests
    {
        private readonly IMapper _mapper = new MapperConfiguration(options => { options.AddProfile<BinanceAutoMapperProfile>(); }).CreateMapper();

        [Fact]
        public async Task Pings()
        {
            // arrange
            var options = Mock.Of<IOptions<BinanceOptions>>();
            using var handler = new FakeHttpMessageHandler(message =>
            {
                Assert.Equal(new Uri("http://example.com/api/v3/ping"), message.RequestUri);

                return new HttpResponseMessage();
            });
            using var http = new HttpClient(handler) { BaseAddress = new Uri("http://example.com") };
            var pool = ObjectPool.Create(new StringBuilderPooledObjectPolicy());
            var client = new BinanceApiClient(options, http, _mapper, pool);

            // act
            var result = await client.PingAsync();

            // assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetsTime()
        {
            // arrange
            var options = Mock.Of<IOptions<BinanceOptions>>();
            using var handler = new FakeHttpMessageHandler(message =>
            {
                Assert.Equal(new Uri("http://example.com/api/v3/time"), message.RequestUri);

                return new HttpResponseMessage()
                {
                    Content = new StringContent("{ \"serverTime\": 1499827319559 }")
                };
            });
            using var http = new HttpClient(handler) { BaseAddress = new Uri("http://example.com") };
            var pool = ObjectPool.Create(new StringBuilderPooledObjectPolicy());
            var client = new BinanceApiClient(options, http, _mapper, pool);

            // act
            var result = await client.GetTimeAsync();

            // assert
            Assert.Equal(DateTime.Parse("2017-07-12T02:41:59.5590000Z", CultureInfo.InvariantCulture).ToUniversalTime(), result);
        }

        [Fact]
        public async Task GetsExchangeInfo()
        {
            // arrange
            var options = Mock.Of<IOptions<BinanceOptions>>();
            using var handler = new FakeHttpMessageHandler(message =>
            {
                Assert.Equal(new Uri("http://example.com/api/v3/exchangeInfo"), message.RequestUri);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(@"
                    {
                        ""timezone"": ""UTC"",
                        ""serverTime"": 1565246363776,
                        ""rateLimits"":
                        [{
                            ""rateLimitType"": ""REQUEST_WEIGHT"",
                            ""interval"": ""MINUTE"",
                            ""intervalNum"": 1,
                            ""limit"": 1000
                        }],
                        ""exchangeFilters"":
                        [{
                            ""filterType"": ""EXCHANGE_MAX_NUM_ORDERS"",
                            ""maxNumOrders"": 1000
                        }],
                        ""symbols"": [
                        {
                            ""symbol"": ""ETHBTC"",
                            ""status"": ""TRADING"",
                            ""baseAsset"": ""ETH"",
                            ""baseAssetPrecision"": 8,
                            ""quoteAsset"": ""BTC"",
                            ""quotePrecision"": 8,
                            ""quoteAssetPrecision"": 8,
                            ""orderTypes"": [
                                ""LIMIT"",
                                ""LIMIT_MAKER"",
                                ""MARKET"",
                                ""STOP_LOSS"",
                                ""STOP_LOSS_LIMIT"",
                                ""TAKE_PROFIT"",
                                ""TAKE_PROFIT_LIMIT""
                            ],
                            ""icebergAllowed"": true,
                            ""ocoAllowed"": true,
                            ""isSpotTradingAllowed"": true,
                            ""isMarginTradingAllowed"": true,
                            ""filters"":
                            [{
                                ""filterType"": ""PRICE_FILTER"",
                                ""minPrice"": ""0.00000100"",
                                ""maxPrice"": ""100000.00000000"",
                                ""tickSize"": ""0.00000100""
                            }],
                            ""permissions"": [
                                ""SPOT"",
                                ""MARGIN""
                            ]
                        }]
                    }")
                };
            });
            using var http = new HttpClient(handler) { BaseAddress = new Uri("http://example.com") };
            var pool = ObjectPool.Create(new StringBuilderPooledObjectPolicy());
            var client = new BinanceApiClient(options, http, _mapper, pool);

            // act
            var result = await client.GetExchangeInfoAsync();

            // assert
            Assert.Equal("UTC", result.Timezone);
            Assert.Equal(1565246363776, result.ServerTime);
            Assert.Equal("REQUEST_WEIGHT", result.RateLimits[0].RateLimitType);
            Assert.Equal("MINUTE", result.RateLimits[0].Interval);
            Assert.Equal(1, result.RateLimits[0].IntervalNum);
            Assert.Equal(1000, result.RateLimits[0].Limit);
            Assert.Equal("EXCHANGE_MAX_NUM_ORDERS", result.ExchangeFilters[0].FilterType);
            Assert.Equal(1000, result.ExchangeFilters[0].MaxNumOrders);
            Assert.Equal("ETHBTC", result.Symbols[0].Symbol);
            Assert.Equal("TRADING", result.Symbols[0].Status);
            Assert.Equal("ETH", result.Symbols[0].BaseAsset);
            Assert.Equal(8, result.Symbols[0].BaseAssetPrecision);
            Assert.Equal(8, result.Symbols[0].QuoteAssetPrecision);
            Assert.Collection(result.Symbols[0].OrderTypes,
                x => Assert.Equal("LIMIT", x),
                x => Assert.Equal("LIMIT_MAKER", x),
                x => Assert.Equal("MARKET", x),
                x => Assert.Equal("STOP_LOSS", x),
                x => Assert.Equal("STOP_LOSS_LIMIT", x),
                x => Assert.Equal("TAKE_PROFIT", x),
                x => Assert.Equal("TAKE_PROFIT_LIMIT", x));
            Assert.True(result.Symbols[0].IcebergAllowed);
            Assert.True(result.Symbols[0].OcoAllowed);
            Assert.True(result.Symbols[0].IsSpotTradingAllowed);
            Assert.True(result.Symbols[0].IsMarginTradingAllowed);
            Assert.Equal("PRICE_FILTER", result.Symbols[0].Filters[0].FilterType);
            Assert.Equal(0.000001M, result.Symbols[0].Filters[0].MinPrice);
            Assert.Equal(100000M, result.Symbols[0].Filters[0].MaxPrice);
            Assert.Equal(0.000001M, result.Symbols[0].Filters[0].TickSize);
            Assert.Collection(result.Symbols[0].Permissions,
                x => Assert.Equal("SPOT", x),
                x => Assert.Equal("MARGIN", x));
        }
    }
}