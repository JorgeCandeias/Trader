﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Readyness
{
    internal class ReadynessProvider : IReadynessProvider
    {
        private readonly IServiceProvider _provider;
        private readonly IEnumerable<IReadynessEntry> _entries;

        public ReadynessProvider(IServiceProvider provider, IEnumerable<IReadynessEntry> entries)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        {
            var result = true;

            foreach (var entry in _entries)
            {
                result &= await entry.IsReadyAsync(_provider, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
    }
}