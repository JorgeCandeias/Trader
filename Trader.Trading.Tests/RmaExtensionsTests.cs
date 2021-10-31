using Outcompute.Trader.Trading.Indicators;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class RmaExtensionsTests
    {
        [Fact]
        public void RmaReturnsIterator()
        {
            // act
            var result = Enumerable.Empty<decimal>().Rma(14);

            // assert
            Assert.IsType<RmaIterator>(result);
        }

        [Fact]
        public void RmaWithSelectorReturnsIterator()
        {
            // act
            var result = Enumerable.Empty<double>().Rma(x => (decimal)x, 14);

            // assert
            Assert.IsType<RmaIterator>(result);
        }
    }
}