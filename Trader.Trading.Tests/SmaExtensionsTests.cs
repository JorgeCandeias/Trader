using Outcompute.Trader.Trading.Indicators;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class SmaExtensionsTests
    {
        [Fact]
        public void SmaReturnsIterator()
        {
            // act
            var result = Enumerable.Empty<decimal>().Sma(14);

            // assert
            Assert.IsType<SmaIterator>(result);
        }

        [Fact]
        public void SmaWithSelectorReturnsIterator()
        {
            // act
            var result = Enumerable.Empty<double>().Sma(x => (decimal)x, 14);

            // assert
            Assert.IsType<SmaIterator>(result);
        }
    }
}