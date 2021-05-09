using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using Trader.Models;
using Trader.Trading.Algorithms.Steps;

namespace Trader.Trading.Algorithms.ValueAveraging
{
    internal class ValueAveragingAlgorithm : ITradingAlgorithm
    {
        private readonly string _name;
        private readonly ValueAveragingAlgorithmOptions _options;
        private readonly ILogger _logger;
        private readonly ITraderRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;
        private readonly ITrackingBuyStep _trackingBuyStep;

        public ValueAveragingAlgorithm(string name, IOptionsSnapshot<ValueAveragingAlgorithmOptions> options, ILogger<ValueAveragingAlgorithmOptions> logger, ITraderRepository repository, ITradingService trader, ISystemClock clock, ITrackingBuyStep trackingBuyStep)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options?.Get(_name) ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trackingBuyStep = trackingBuyStep ?? throw new ArgumentNullException(nameof(trackingBuyStep));
        }

        public string Symbol => throw new System.NotImplementedException();

        private static string Type => nameof(ValueAveragingAlgorithm);

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task GoAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("{Type} {Name} running...", Type, _name);

            // grab the symbol information
            // todo: make this a dictionary up front so the algos dont have to enumerate it all the time
            var symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);

            // first calculate a tracking buy
            await _trackingBuyStep
                .GoAsync(symbol, _options.PullbackRatio, _options.TargetQuoteBalanceFractionPerBuy, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}