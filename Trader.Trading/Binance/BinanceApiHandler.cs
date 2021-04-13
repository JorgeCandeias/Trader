using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Trader.Data;
using Trader.Trading.Binance.Signing;

namespace Trader.Trading.Binance
{
    internal class BinanceApiHandler : DelegatingHandler
    {
        private readonly BinanceOptions _options;
        private readonly ISigner _signer;

        public BinanceApiHandler(IOptions<BinanceOptions> options, ISigner signer)
        {
            _options = options.Value;
            _signer = signer;
        }

        private const string ApiKeyHeader = "X-MBX-APIKEY";
        private const string UsedRequestWeightHeaderPrefix = "X-MBX-USED-WEIGHT-";
        private const string UsedOrderCountHeaderPrefix = "X-MBX-ORDER-COUNT-";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await PreProcessAsync(request, cancellationToken);

            var response = await base.SendAsync(request, cancellationToken);

            await HandleBinanceErrorAsync(response, cancellationToken);
            PostProcess(response);

            return response;
        }

        private async Task PreProcessAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // add the api key header
            request.Headers.Add(ApiKeyHeader, _options.ApiKey);

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
                    var text = await content.ReadAsStringAsync(cancellationToken);
                    var hash = _signer.Sign(text);
                    var result = text + "&signature=" + hash;

                    request.Content = new StringContent(result);
                }
            }
        }

        private static async Task HandleBinanceErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            // skip handling for successful results
            if (response.IsSuccessStatusCode) return;

            // attempt graceful handling of a binance api error
            var error = await response.Content.ReadFromJsonAsync<ErrorModel>(null, cancellationToken);
            if (error is not null && error.Code != 0)
            {
                throw new BinanceCodeException(error.Code, error.Msg, response.StatusCode);
            }

            // otherwise default to the standard exception
            response.EnsureSuccessStatusCode();
        }

        private static void PostProcess(HttpResponseMessage response)
        {
            if (!BinanceApiContext.CaptureUsage) return;

            foreach (var header in response.Headers)
            {
                if (header.Key.StartsWith(UsedRequestWeightHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var key = header.Key[UsedRequestWeightHeaderPrefix.Length..];
                    var unit = key[^1..].ToUpperInvariant();
                    var value = int.Parse(key[..^1]);

                    var window = unit switch
                    {
                        "S" => TimeSpan.FromSeconds(value),
                        "M" => TimeSpan.FromMinutes(value),
                        "H" => TimeSpan.FromHours(value),
                        "D" => TimeSpan.FromDays(value),
                        _ => throw new InvalidOperationException()
                    };

                    foreach (var item in header.Value)
                    {
                        var weight = int.Parse(item);

                        BinanceApiContext.Usage?.Add(new Usage(RateLimitType.RequestWeight, window, weight));
                    }
                }
                else if (header.Key.StartsWith(UsedOrderCountHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var key = header.Key[UsedOrderCountHeaderPrefix.Length..];
                    var unit = key[^1..].ToUpperInvariant();
                    var value = int.Parse(key[..^1]);

                    var window = unit switch
                    {
                        "S" => TimeSpan.FromSeconds(value),
                        "M" => TimeSpan.FromMinutes(value),
                        "H" => TimeSpan.FromHours(value),
                        "D" => TimeSpan.FromDays(value),
                        _ => throw new InvalidOperationException()
                    };

                    foreach (var item in header.Value)
                    {
                        var count = int.Parse(item);

                        BinanceApiContext.Usage?.Add(new Usage(RateLimitType.Orders, window, count));
                    }
                }
            }
        }
    }
}