using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Binance.Signing;

namespace Outcompute.Trader.Trading.Binance.Handlers;

internal class BinanceApiSigningPreHandler : DelegatingHandler
{
    private readonly BinanceOptions _options;
    private readonly ISigner _signer;

    public BinanceApiSigningPreHandler(IOptions<BinanceOptions> options, ISigner signer)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // add the api key header
        request.Headers.Add(_options.ApiKeyHeader, _options.ApiKey);

        if (!BinanceApiContext.SkipSigning)
        {
            // sign query content
            if (request.RequestUri?.Query?.Length > 1)
            {
                var hash = _signer.Sign(request.RequestUri.Query[1..]);

                var builder = new UriBuilder(request.RequestUri)
                {
                    Query = request.RequestUri.Query + "&signature=" + hash
                };

                request.RequestUri = builder.Uri;
            }

            // sign form content
            else if (request.Content is FormUrlEncodedContent content)
            {
                var text = await content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

                var hash = _signer.Sign(text);
                var result = text + "&signature=" + hash;

                request.Content = new StringContent(result);
            }
        }

        return await base
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
    }
}