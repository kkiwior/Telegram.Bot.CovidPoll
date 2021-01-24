using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll.Services.HostedServices
{
    public class QueueHostedService : BackgroundService
    {
        private readonly IQueueService queueService;
        private readonly ILogger<QueueHostedService> log;

        public QueueHostedService(IQueueService queueService, ILogger<QueueHostedService> log)
        {
            this.queueService = queueService;
            this.log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await queueService.DequeueAsync(stoppingToken);
                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception ex)
                {
                    log.LogError(
                        ex, $"[{nameof(QueueHostedService)}] - Error occurred executing {workItem}.");
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
