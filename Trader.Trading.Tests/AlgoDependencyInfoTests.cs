using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System.Linq;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoDependencyInfoTests
    {
        [Fact]
        public void GetsTickers()
        {
            // arrange
            var algo1 = new AlgoHostGrainOptions();
            algo1.DependsOn.Tickers.Add("Ticker1");
            algo1.DependsOn.Tickers.Add("Ticker2");

            var algo2 = new AlgoHostGrainOptions();
            algo2.DependsOn.Tickers.Add("Ticker2");
            algo2.DependsOn.Tickers.Add("Ticker3");

            var options = new AlgoManagerGrainOptions()
            {
                Algos =
                {
                    { "Algo1", algo1 },
                    { "Algo2", algo2 }
                }
            };

            var monitor = Mock.Of<IOptionsMonitor<AlgoManagerGrainOptions>>(x => x.CurrentValue == options);
            var info = new AlgoDependencyInfo(monitor);

            // act
            var result = info.GetTickers().ToList();

            // assert
            Assert.Equal(3, result.Count);
            Assert.Contains("Ticker1", result);
            Assert.Contains("Ticker2", result);
            Assert.Contains("Ticker3", result);
        }

        [Fact]
        public void GetsKlines()
        {
            // arrange
            var algo1 = new AlgoHostGrainOptions();
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol1", Interval = KlineInterval.Days1, Periods = 10 });
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol1", Interval = KlineInterval.Hours1, Periods = 20 });
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol2", Interval = KlineInterval.Months1, Periods = 3 });

            var algo2 = new AlgoHostGrainOptions();
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol1", Interval = KlineInterval.Days1, Periods = 20 });
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol2", Interval = KlineInterval.Months1, Periods = 2 });
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol3", Interval = KlineInterval.Minutes1, Periods = 100 });

            var options = new AlgoManagerGrainOptions()
            {
                Algos =
                {
                    { "Algo1", algo1 },
                    { "Algo2", algo2 }
                }
            };

            var monitor = Mock.Of<IOptionsMonitor<AlgoManagerGrainOptions>>(x => x.CurrentValue == options);
            var info = new AlgoDependencyInfo(monitor);

            // act
            var result = info.GetKlines().ToList();

            // assert
            Assert.Equal(6, result.Count);
            Assert.Contains(new KlineDependency("Symbol1", KlineInterval.Days1, 20), result);
            Assert.Contains(new KlineDependency("Symbol1", KlineInterval.Hours1, 20), result);
            Assert.Contains(new KlineDependency("Symbol2", KlineInterval.Months1, 3), result);
            Assert.Contains(new KlineDependency("Symbol1", KlineInterval.Days1, 20), result);
            Assert.Contains(new KlineDependency("Symbol2", KlineInterval.Months1, 2), result);
            Assert.Contains(new KlineDependency("Symbol3", KlineInterval.Minutes1, 100), result);
        }

        [Fact]
        public void GetsKlinesWithFilter()
        {
            // arrange
            var algo1 = new AlgoHostGrainOptions();
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol1", Interval = KlineInterval.Days1, Periods = 10 });
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol1", Interval = KlineInterval.Hours1, Periods = 20 });
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol2", Interval = KlineInterval.Months1, Periods = 3 });

            var algo2 = new AlgoHostGrainOptions();
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol1", Interval = KlineInterval.Days1, Periods = 20 });
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol2", Interval = KlineInterval.Months1, Periods = 2 });
            algo1.DependsOn.Klines.Add(new AlgoHostGrainOptionsKline { Symbol = "Symbol3", Interval = KlineInterval.Minutes1, Periods = 100 });

            var options = new AlgoManagerGrainOptions()
            {
                Algos =
                {
                    { "Algo1", algo1 },
                    { "Algo2", algo2 }
                }
            };

            var monitor = Mock.Of<IOptionsMonitor<AlgoManagerGrainOptions>>(x => x.CurrentValue == options);
            var info = new AlgoDependencyInfo(monitor);

            // act
            var result = info.GetKlines("Symbol2", KlineInterval.Months1).ToList();

            // assert
            Assert.Equal(2, result.Count);
            Assert.Contains(new KlineDependency("Symbol2", KlineInterval.Months1, 3), result);
            Assert.Contains(new KlineDependency("Symbol2", KlineInterval.Months1, 2), result);
        }
    }
}