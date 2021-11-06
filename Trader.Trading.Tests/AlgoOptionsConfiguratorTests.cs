using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Algorithms;
using System.Collections.Generic;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoOptionsConfiguratorTests
    {
        private class TestOptions
        {
            public string MyValue { get; set; } = string.Empty;
        }

        [Fact]
        public void ConfiguresNamedOptions()
        {
            // arrange
            var name = "TestName";
            var value = "TestValue";
            var mapping = new AlgoConfigurationMappingOptions();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { $"{mapping.AlgosKey}:{name}:{mapping.AlgoOptionsSubKey}:MyValue", value }
                })
                .Build();
            var configurator = new AlgoUserOptionsConfigurator<TestOptions>(Options.Create(mapping), config);
            var options = new TestOptions();

            // act
            configurator.Configure(name, options);

            // assert
            Assert.Equal(value, options.MyValue);
        }
    }
}