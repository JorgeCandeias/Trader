using Microsoft.Extensions.Options;

namespace Outcompute.Trader.Trading.Binance.Handlers;

internal class BinanceApiConcurrencyHandler : DelegatingHandler
{
    public BinanceApiConcurrencyHandler(IOptions<BinanceOptions> options)
    {
        _semaphore = new(options.Value.MaxConcurrentApiRequests);
    }

    private readonly SemaphoreSlim _semaphore;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await _semaphore
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            return await base
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}