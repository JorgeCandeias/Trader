using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading
{
    public interface IUserDataStreamClient
    {
        bool IsConnected { get; }

        Task ConnectAsync(string listenKey, CancellationToken cancellationToken = default);

        Task CloseAsync(CancellationToken cancellationToken = default);

        Task<UserDataStreamMessage> ReceiveAsync(CancellationToken cancellationToken = default);
    }
}