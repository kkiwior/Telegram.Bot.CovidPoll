using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Telegram.Bot.CovidPoll.Services
{
    public class QueueService
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> workItems;
        private SemaphoreSlim signal;

        public QueueService(BotClientService botClientService)
        {
            this.workItems = new ConcurrentQueue<Func<CancellationToken, Task>>();
            this.signal = new SemaphoreSlim(0);
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            workItems.Enqueue(workItem);
            signal.Release();
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await signal.WaitAsync(cancellationToken);
            workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }
}
