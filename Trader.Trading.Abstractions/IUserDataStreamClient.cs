using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading
{
    public interface IUserDataStreamClient : IDisposable
    {
        Task ConnectAsync(CancellationToken cancellationToken = default);

        Task CloseAsync(CancellationToken cancellationToken = default);

        Task<UserDataStreamMessage> ReceiveAsync(CancellationToken cancellationToken = default);
    }
}