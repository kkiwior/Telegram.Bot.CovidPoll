using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Services.Interfaces;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Extensions;

namespace Telegram.Bot.CovidPoll.Services.HostedServices
{
    public class BotPollSenderHostedService : BackgroundService
    {
        private readonly IOptions<BotSettings> botSettings;
        private readonly IBotPollResultSenderService botPollResultSenderService;
        private readonly ITaskDelayProvider taskDelayProvider;
        private readonly IBotPollSenderService botPollSenderService;
        private readonly IDateProvider dateProvider;

        public BotPollSenderHostedService(IOptions<BotSettings> botSettings, 
            IBotPollResultSenderService botPollResultSenderService, ITaskDelayProvider taskDelayProvider, 
            IBotPollSenderService botPollSenderService, IDateProvider dateProvider)
        {
            this.botSettings = botSettings;
            this.botPollResultSenderService = botPollResultSenderService;
            this.taskDelayProvider = taskDelayProvider;
            this.botPollSenderService = botPollSenderService;
            this.dateProvider = dateProvider;
        }

        private DateTimeOffset PollsStart { get; set; }

        private DateTimeOffset PollsEnd { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var date = dateProvider.DateTimeOffsetUtcNow().ConvertUtcToPolishTime().Midnight();

            PollsStart = date.AddHours(botSettings.Value.PollsStartHour).ToUniversalTime();
            PollsEnd = date.AddHours(botSettings.Value.PollsEndHour).ToUniversalTime();

            await WorkerAsync(stoppingToken);
        }

        private async Task WorkerAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (dateProvider.DateTimeOffsetUtcNow() >= PollsStart && 
                    dateProvider.DateTimeOffsetUtcNow() < PollsEnd)
                {
                    var pollsResult = await botPollSenderService.SendPolls(stoppingToken);
                    if (pollsResult)
                    {
                        PollsStart = PollsStart.AddDays(1);
                    }
                    else
                    {
                        await taskDelayProvider.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                }
                else if (dateProvider.DateTimeOffsetUtcNow() >= PollsEnd)
                {
                    await botPollSenderService.StopPolls(stoppingToken);
                    PollsEnd = PollsEnd.AddDays(1);

                    botPollResultSenderService.SendPredictionsToChats();
                }
                await taskDelayProvider.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}