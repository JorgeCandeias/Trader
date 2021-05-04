using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;

namespace Trader.Trading.Binance.Handlers
{
    internal class BinanceApiCircuitBreakerHandler : DelegatingHandler
    {
        private readonly ISystemClock _clock;

        public BinanceApiCircuitBreakerHandler(ISystemClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // prevent the request from going through if the api is overloaded
            if (_clock.UtcNow <= _retryAfterUtc)
            {
                throw new BinanceTooManyRequestsException(_retryAfterUtc);
            }

            try
            {
                return await base
                    .SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (BinanceTooManyRequestsException ex)
            {
                // safely bump the opening window forward
                lock (_lock)
                {
                    if (ex.RetryAfterUtc > _retryAfterUtc)
                    {
                        _retryAfterUtc = ex.RetryAfterUtc;
                    }
                }

                // escalate to the caller regardless
                throw;
            }
        }

        private readonly object _lock = new();
        private DateTime _retryAfterUtc = DateTime.MinValue;
    }
}