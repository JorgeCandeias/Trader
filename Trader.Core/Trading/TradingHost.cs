using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Trading.Algorithms;
using Trader.Core.Trading.Algorithms.Step;

namespace Trader.Core.Trading
{
    internal class TradingHost : ITradingHost, IHostedService
    {
        private readonly ILogger _logger;
        private readonly IStepAlgorithmFactory _factory;

        public TradingHost(ILogger<TradingHost> logger, IStepAlgorithmFactory factory)
        {
            _logger = logger;
            _factory = factory;
        }

        private static string Name => nameof(TradingHost);

        private readonly List<ITradingAlgorithm> _algos = new();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} starting...", Name);

            _algos.Add(_factory.Create("BTCGBP-1"));
            _algos.Add(_factory.Create("ETHGBP-1"));
            _algos.Add(_factory.Create("ADAGBP-1"));
            _algos.Add(_factory.Create("XRPGBP-1"));
            _algos.Add(_factory.Create("LINKGBP-1"));
            _algos.Add(_factory.Create("DOGEGBP-1"));
            _algos.Add(_factory.Create("SXPGBP-1"));
            _algos.Add(_factory.Create("DOTGBP-1"));

            foreach (var algo in _algos)
            {
                await algo.StartAsync(cancellationToken);
            }

            _logger.LogInformation("{Name} started", Name);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} stopping...", Name);

            foreach (var algo in _algos)
            {
                await algo.StopAsync(cancellationToken);
            }

            _logger.LogInformation("{Name} stopped", Name);
        }
    }
}