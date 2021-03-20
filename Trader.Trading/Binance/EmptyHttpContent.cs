using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Trader.Trading.Binance
{
    internal class EmptyHttpContent : HttpContent
    {
        private EmptyHttpContent()
        {
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return Task.CompletedTask;
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }

        public static EmptyHttpContent Instance { get; } = new EmptyHttpContent();
    }
}