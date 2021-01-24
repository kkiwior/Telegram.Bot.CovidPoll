using System.Threading;
using System.Threading.Tasks;

namespace Telegram.Bot.CovidPoll.Services.Interfaces
{
    public interface IBotPollSenderService
    {
        Task<bool> SendPolls(CancellationToken stoppingToken);
        Task StopPolls(CancellationToken stoppingToken);
    }
}