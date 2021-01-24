using System;
using System.Threading;
using System.Threading.Tasks;

namespace Telegram.Bot.CovidPoll.Helpers.Interfaces
{
    public interface ITaskDelayHelper
    {
        Task Delay(TimeSpan time, CancellationToken stoppingToken);
    }
}