using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Converters;
using System;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    public class AccountTypeConverterTests
    {
        [Theory]
        [InlineData(null, AccountType.None)]
        [InlineData("SPOT", AccountType.Spot)]
        public void ConvertsFromStringToAccountType(string source, AccountType expected)
        {
            // arrange
            var provider = new ServiceCollection()
                .AddSingleton<AccountTypeConverter>()
                .AddAutoMapper(config =>
                {
                    config.CreateMap<string, AccountType>().ConvertUsing<AccountTypeConverter>();
                })
                .BuildServiceProvider();

            var mapper = provider.GetRequiredService<IMapper>();

            // act
            var result = mapper.Map<AccountType>(source);

            // assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ThrowsOnInvalidString()
        {
            // arrange
            var provider = new ServiceCollection()
                .AddSingleton<AccountTypeConverter>()
                .AddAutoMapper(config =>
                {
                    config.CreateMap<string, AccountType>().ConvertUsing<AccountTypeConverter>();
                })
                .BuildServiceProvider();

            var mapper = provider.GetRequiredService<IMapper>();

            // act
            void Test() => mapper.Map<AccountType>("");

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("source", Test);
        }

        [Theory]
        [InlineData(AccountType.None, null)]
        [InlineData(AccountType.Spot, "SPOT")]
        public void ConvertsFromAccountTypeToString(AccountType source, string expected)
        {
            // arrange
            var provider = new ServiceCollection()
                .AddSingleton<AccountTypeConverter>()
                .AddAutoMapper(config =>
                {
                    config.CreateMap<AccountType, string>().ConvertUsing<AccountTypeConverter>();
                })
                .BuildServiceProvider();

            var mapper = provider.GetRequiredService<IMapper>();

            // act
            var result = mapper.Map<string>(source);

            // assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ThrowsOnInvalidAccountType()
        {
            // arrange
            var provider = new ServiceCollection()
                .AddSingleton<AccountTypeConverter>()
                .AddAutoMapper(config =>
                {
                    config.CreateMap<AccountType, string>().ConvertUsing<AccountTypeConverter>();
                })
                .BuildServiceProvider();

            var mapper = provider.GetRequiredService<IMapper>();

            // act
            void Test() => mapper.Map<string>((AccountType)999);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("source", Test);
        }
    }
}