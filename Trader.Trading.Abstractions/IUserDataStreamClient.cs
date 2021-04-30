using System;
using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading
{
    public interface IUserDataStreamClient : IDisposable
    {
        Task ConnectAsync(CancellationToken cancellationToken = default);

        Task CloseAsync(CancellationToken cancellationToken = default);

        Task<UserDataStreamMessage> ReceiveAsync(CancellationToken cancellationToken = default);
    }
}