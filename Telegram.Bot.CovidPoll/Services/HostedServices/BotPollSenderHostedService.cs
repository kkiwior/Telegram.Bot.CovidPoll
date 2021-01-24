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
        private readonly IBotPollResultSenderService botPollResultSender;
        private readonly ITaskDelayHelper taskDelayHelper;
        private readonly IBotPollSenderService botPollSenderService;

        public BotPollSenderHostedService(
            IOptions<BotSettings> botSettings, IBotPollResultSenderService botPollResultSender,
            ITaskDelayHelper taskDelayHelper, IBotPollSenderService botPollSenderService)
        {
            this.botSettings = botSettings;
            this.botPollResultSender = botPollResultSender;
            this.taskDelayHelper = taskDelayHelper;
            this.botPollSenderService = botPollSenderService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pollsStart = DateTimeOffset.UtcNow
                .ConvertUtcToPolishTime().Date.AddHours(botSettings.Value.PollsStartHour)
                .ToUniversalTime();

            var pollsEnd = DateTimeOffset.UtcNow
                .ConvertUtcToPolishTime().Date.AddHours(botSettings.Value.PollsEndHour)
                .ToUniversalTime();

            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= pollsStart && DateTime.UtcNow <= pollsEnd)
                {
                    var pollsResult = await botPollSenderService.SendPolls(stoppingToken);
                    if (pollsResult)
                    {
                        pollsStart = DateTimeOffset.UtcNow.ConvertUtcToPolishTime().Date
                            .AddDays(1)
                            .AddHours(botSettings.Value.PollsStartHour)
                            .ToUniversalTime();
                    }
                    else
                    {
                        await taskDelayHelper.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                }
                else if (DateTime.UtcNow >= pollsEnd)
                {
                    await botPollSenderService.StopPolls(stoppingToken);
                    pollsEnd = DateTimeOffset.UtcNow.ConvertUtcToPolishTime().Date
                        .AddDays(1)
                        .AddHours(botSettings.Value.PollsEndHour)
                        .ToUniversalTime();

                    botPollResultSender.SendPredictionsToChats();
                }
                await taskDelayHelper.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}