using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    internal class AlgoContextTickerConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        private readonly ITickerProvider _tickers;

        public AlgoContextTickerConfigurator(ITickerProvider tickers)
        {
            _tickers = tickers;
        }

        public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
        {
            if (IsNullOrEmpty(context.Symbol.Name))
            {
                return;
            }

            context.Ticker = await _tickers
                .GetRequiredTickerAsync(context.Symbol.Name, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}