using System;
using System.Threading;
using System.Threading.Tasks;

namespace Telegram.Bot.CovidPoll.Services.Interfaces
{
    public interface IQueueService
    {
        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
    }
}