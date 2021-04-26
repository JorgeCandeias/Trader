using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Trader.Trading.Binance.Signing
{
    internal class Signer : ISigner
    {
        private readonly byte[] _key;

        public Signer(IOptions<BinanceOptions> options)
        {
            // cache the key bytes for repeated use
            _key = Encoding.UTF8.GetBytes(options.Value.SecretKey);
        }

        /// <summary>
        /// Each thread pool thread keeps its own hmac to avoid transient allocations.
        /// </summary>
        [ThreadStatic]
        private static HMACSHA256? _hmac;

        /// <summary>
        /// Creates a signature for the specified payload.
        /// </summary>
        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "API")]
        public string Sign(string value)
        {
            var hmac = GetOrCreateHmac(_key);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
            var signature = BitConverter.ToString(hash).Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();

            return signature;
        }

        /// <summary>
        /// Gets or creates the hmac algo for the current thread.
        /// </summary>
        private static HMACSHA256 GetOrCreateHmac(byte[] key)
        {
            if (_hmac is null)
            {
                _hmac = new HMACSHA256(key);
            }

            return _hmac;
        }
    }
}