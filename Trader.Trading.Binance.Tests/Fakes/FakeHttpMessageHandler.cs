namespace Outcompute.Trader.Trading.Binance.Tests.Fakes
{
    internal class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _action;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> action)
        {
            _action = action;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_action(request));
        }
    }
}