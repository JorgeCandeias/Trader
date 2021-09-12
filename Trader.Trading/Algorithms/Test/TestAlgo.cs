using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Test
{
    internal class TestAlgo : IAlgo
    {
        private readonly IOptionsMonitor<TestAlgoOptions> _options;
        private readonly ILogger _logger;
        private readonly IAlgoContext _context;

        public TestAlgo(IOptionsMonitor<TestAlgoOptions> options, ILogger<TestAlgo> logger, IAlgoContext context)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task GoAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("My name is {Name} and my options are {Options}", _context.Name, _options.Get(_context.Name));

            return Task.CompletedTask;
        }
    }
}