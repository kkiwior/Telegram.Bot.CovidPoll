using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Extensions;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll.Services.HostedServices
{
    public class CovidTrackingHostedService : BackgroundService
    {
        private readonly IOptions<CovidTrackingSettings> covidTrackingSettings;
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly ILogger<CovidTrackingHostedService> log;
        private readonly ICovidDownloadingService covidDownloadingService;
        private readonly ITaskDelayHelper taskDelayHelper;

        public CovidTrackingHostedService(IOptions<CovidTrackingSettings> covidTrackingSettings, 
            IHostApplicationLifetime applicationLifetime, ILogger<CovidTrackingHostedService> log, 
            ICovidDownloadingService covidDownloadingService, ITaskDelayHelper taskDelayHelper)
        {
            this.covidTrackingSettings = covidTrackingSettings;
            this.applicationLifetime = applicationLifetime;
            this.log = log;
            this.covidDownloadingService = covidDownloadingService;
            this.taskDelayHelper = taskDelayHelper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var fetchDate = DateTimeOffset.UtcNow
                .ConvertUtcToPolishTime().Date.AddHours(covidTrackingSettings.Value.FetchDataHour)
                .ToUniversalTime();

            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= fetchDate)
                {
                    try
                    {
                        var result = await covidDownloadingService.DownloadCovidByJsonAsync();
                        if (result)
                        {
                            fetchDate = DateTime.UtcNow.Date.AddDays(1)
                                .AddHours(covidTrackingSettings.Value.FetchDataHour);
                            log.LogInformation(
                                $"[{nameof(CovidTrackingHostedService)}]: Data successfully downloaded" +
                                "or is up to date");
                        }
                        else
                        {
                            log.LogError(
                                @$"[{nameof(CovidTrackingHostedService)}]: Problem with downloading data");
                            await taskDelayHelper.Delay(TimeSpan.FromMinutes(20), stoppingToken);
                        }
                    }
                    catch (CovidParseException ex)
                    {
                        log.LogError(ex, $"[{nameof(CovidTrackingHostedService)}]: CovidParseException");
                        applicationLifetime.StopApplication();
                    }
                }
                await taskDelayHelper.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
