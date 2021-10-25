using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.InMemory.UserData
{
    public interface IInMemoryUserDataStreamSender
    {
        IDisposable Register(Func<UserDataStreamMessage, CancellationToken, Task> action);

        Task SendAsync(UserDataStreamMessage message, CancellationToken cancellationToken = default);
    }
}