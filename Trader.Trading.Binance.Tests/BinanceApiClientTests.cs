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
        public async Task GetsTimeAsync()
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
    }
}