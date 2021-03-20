using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Trader.Trading.Binance.Signing
{
    internal class Signer : ISigner
    {
        private readonly BinanceOptions _options;

        public Signer(IOptions<BinanceOptions> options)
        {
            _options = options.Value;
        }

        [ThreadStatic]
        private static HMACSHA256? _hmac;

        public string Sign(string value)
        {
            var hmac = GetOrCreateHmac(_options.SecretKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
            var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            return signature;
        }

        private static HMACSHA256 GetOrCreateHmac(string key)
        {
            if (_hmac is null)
            {
                _hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            }

            return _hmac;
        }
    }
}