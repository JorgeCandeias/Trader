using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class ProfitTests
    {
        [Fact]
        public void Zero()
        {
            // arrange
            var symbol = "ABCXYZ";
            var asset = "ABC";
            var quote = "XYZ";

            // act
            var zero = Profit.Zero(symbol, asset, quote);

            // arrange
            Assert.Equal(symbol, zero.Symbol);
            Assert.Equal(asset, zero.Asset);
            Assert.Equal(quote, zero.Quote);
            Assert.Equal(0, zero.Today);
            Assert.Equal(0, zero.Yesterday);
            Assert.Equal(0, zero.ThisWeek);
            Assert.Equal(0, zero.PrevWeek);
            Assert.Equal(0, zero.ThisMonth);
            Assert.Equal(0, zero.ThisYear);
            Assert.Equal(0, zero.All);
            Assert.Equal(0, zero.D1);
            Assert.Equal(0, zero.D7);
            Assert.Equal(0, zero.D30);
        }

        [Fact]
        public void Aggregate()
        {
            // arrange
            var symbol = "ABCXYZ";
            var asset = "ABC";
            var quote = "XYZ";
            var profit1 = Profit.Zero(symbol, asset, quote) with { Today = 1 };
            var profit2 = Profit.Zero(symbol, asset, quote) with { Today = 2 };

            // act
            var result = Profit.Aggregate(new[] { profit1, profit2 });

            // assert
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(asset, result.Asset);
            Assert.Equal(quote, result.Quote);
            Assert.Equal(3, result.Today);
        }

        [Fact]
        public void Add()
        {
            // arrange
            var symbol = "ABCXYZ";
            var asset = "ABC";
            var quote = "XYZ";
            var profit1 = Profit.Zero(symbol, asset, quote) with { Today = 1 };
            var profit2 = Profit.Zero(symbol, asset, quote) with { Today = 2 };

            // act
            var result = profit1.Add(profit2);

            // assert
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(asset, result.Asset);
            Assert.Equal(quote, result.Quote);
            Assert.Equal(3, result.Today);
        }

        [Fact]
        public void FromEvents()
        {
            // arrange
            var symbol = Symbol.Empty with { Name = "ABCXYZ", BaseAsset = "ABC", QuoteAsset = "XYZ" };
            var time = DateTime.Today;
            var profit = new ProfitEvent(symbol, time, 1, 1, 2, 2, 100m, 123m, 124m);
            var commission = new CommissionEvent(symbol, time, 2, 2, "XYZ", 0.01m);

            // act
            var result = Profit.FromEvents(symbol, new[] { profit }, new[] { commission }, DateTime.Today);

            // assert
            Assert.Equal(symbol.Name, result.Symbol);
            Assert.Equal(99.99m, result.Today);
        }
    }
}