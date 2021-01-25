using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public class TaskDelayProvider : ITaskDelayProvider
    {
        public Task Delay(TimeSpan time, CancellationToken stoppingToken)
        {
            return Task.Delay(time, stoppingToken);
        }
    }
}
