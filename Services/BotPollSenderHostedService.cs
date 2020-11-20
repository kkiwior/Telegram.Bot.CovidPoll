using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Config;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotPollSenderHostedService : BackgroundService
    {
        private readonly BotClientService botClientService;
        private readonly IOptions<BotSettings> botSettings;
        public BotPollSenderHostedService(BotClientService botClientService, IOptions<BotSettings> botSettings)
        {
            this.botClientService = botClientService;
            this.botSettings = botSettings;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }
        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            var pollsStart = DateTime.UtcNow.Date.AddHours(botSettings.Value.PollsStartHourUtc);
            var pollsEnd = DateTime.UtcNow.Date.AddHours(botSettings.Value.PollsEndHourUtc);
            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= pollsStart)
                {

                    pollsStart = DateTime.UtcNow.Date.AddDays(1).AddHours(botSettings.Value.PollsStartHourUtc);
                }
                else if (DateTime.UtcNow >= pollsEnd)
                {

                    pollsEnd = DateTime.UtcNow.Date.AddDays(1).AddHours(botSettings.Value.PollsEndHourUtc);
                }
            }
        }
    }
}
