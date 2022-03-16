using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators
{
    public static class HL2Tests
    {
        [Fact]
        public static void YieldsOutput()
        {
            // arrange
            using var indicator = new HL2()
            {
                new HL(10, 20),
                new HL(10, 30),
                new HL(20, 30)
            };

            // assert
            Assert.Collection(indicator,
                x => Assert.Equal(15, x),
                x => Assert.Equal(20, x),
                x => Assert.Equal(25, x));
        }
    }
}