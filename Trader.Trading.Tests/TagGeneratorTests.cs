using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Commands;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class TagGeneratorTests
    {
        [Fact]
        public void Generates()
        {
            // arrange
            var symbol = "ABCXYZ";
            var price = 123.4567M;
            var options = Options.Create(new TagGeneratorOptions());
            var generator = new TagGenerator(options);

            // act
            var result = generator.Generate(symbol, price);

            // assert
            Assert.Equal("ABCXYZ12345670000", result);
        }
    }
}