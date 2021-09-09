using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Outcompute.Trader.Core.Time;

namespace Outcompute.Trader.Trading.Binance.Handlers
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
            var wait = _retryAfterUtc.Subtract(_clock.UtcNow);
            if (wait > TimeSpan.Zero)
            {
                throw new BinanceTooManyRequestsException(wait);
            }

            try
            {
                return await base
                    .SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (BinanceTooManyRequestsException ex)
            {
                var retryAfterUtc = _clock.UtcNow.Add(ex.RetryAfter);
                if (retryAfterUtc > _retryAfterUtc)
                {
                    _retryAfterUtc = retryAfterUtc;
                }

                // escalate to the caller regardless
                throw;
            }
        }

        private DateTime _retryAfterUtc = DateTime.MinValue;
    }
}