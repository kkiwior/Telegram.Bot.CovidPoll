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

        public BotPollSenderHostedService(IOptions<BotSettings> botSettings, 
            IBotPollResultSenderService botPollResultSender, ITaskDelayHelper taskDelayHelper, 
            IBotPollSenderService botPollSenderService)
        {
            this.botSettings = botSettings;
            this.botPollResultSender = botPollResultSender;
            this.taskDelayHelper = taskDelayHelper;
            this.botPollSenderService = botPollSenderService;
        }

        private DateTimeOffset PollsStart { get; set; }

        private DateTimeOffset PollsEnd { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PollsStart = DateTimeOffset.UtcNow
                .ConvertUtcToPolishTime().Date.AddHours(botSettings.Value.PollsStartHour)
                .ToUniversalTime();

            PollsEnd = DateTimeOffset.UtcNow
                .ConvertUtcToPolishTime().Date.AddHours(botSettings.Value.PollsEndHour)
                .ToUniversalTime();

            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= PollsStart && DateTime.UtcNow <= PollsEnd)
                {
                    var pollsResult = await botPollSenderService.SendPolls(stoppingToken);
                    if (pollsResult)
                    {
                        PollsStart = PollsStart.AddDays(1);
                    }
                    else
                    {
                        await taskDelayHelper.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                }
                else if (DateTime.UtcNow >= PollsEnd)
                {
                    await botPollSenderService.StopPolls(stoppingToken);
                    PollsEnd = PollsEnd.AddDays(1);

                    botPollResultSender.SendPredictionsToChats();
                }
                await taskDelayHelper.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}