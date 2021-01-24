﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll.Services
{
    public class QueueHostedService : BackgroundService
    {
        private readonly IQueueService queueService;

        public QueueHostedService(IQueueService queueService)
        {
            this.queueService = queueService;
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
                    //Log.Error(ex, $"[{nameof(QueueHostedService)}] - Error occurred executing {workItem}.");
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
