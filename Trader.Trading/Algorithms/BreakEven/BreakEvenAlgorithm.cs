using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading.Algorithms.BreakEven
{
    internal class BreakEvenAlgorithm : IBreakEvenAlgorithm
    {
        private readonly string _name;
        private readonly BreakEvenAlgorithmOptions _options;
        private readonly ILogger _logger;

        public BreakEvenAlgorithm(string name, IOptionsSnapshot<BreakEvenAlgorithmOptions> options, ILogger<BreakEvenAlgorithm> logger)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options.Get(name) ?? throw new ArgumentNullException(nameof(logger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static string Type => nameof(BreakEvenAlgorithm);
        public string Symbol => _options.Symbol;

        public Task GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("{Type} {Name} starting...", Type, _name);

            // resolve all significant buy orders

            // resolve desired sell orders

            // cancel all sell orders that are not desired

            // create all desired sell orders not created yet

            return Task.CompletedTask;
        }

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Profit.Zero);
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Statistics.Zero);
        }
    }
}