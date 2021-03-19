using Microsoft.Extensions.Options;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Core.Trading.Algorithms.BreakEven
{
    internal class BreakEvenAlgorithm : IBreakEvenAlgorithm
    {
        private readonly string _name;
        private readonly BreakEvenAlgorithmOptions _options;

        public BreakEvenAlgorithm(string name, IOptionsSnapshot<BreakEvenAlgorithmOptions> options)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options.Get(name);
        }

        public string Symbol => _options.Symbol;

        public ValueTask<ImmutableList<AccountTrade>> GetTradesAsync()
        {
            return new ValueTask<ImmutableList<AccountTrade>>(ImmutableList<AccountTrade>.Empty);
        }

        public Task GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo, CancellationToken cancellationToken = default)
        {
            // todo: resolve all significant buy orders

            // todo: resolve desired sell orders

            // todo: cancel all sell orders that are not desired

            // todo: create all desired sell orders not created yet


            return Task.CompletedTask;
        }
    }
}