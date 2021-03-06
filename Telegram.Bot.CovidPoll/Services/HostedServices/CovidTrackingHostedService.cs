﻿using Microsoft.Extensions.Hosting;
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
        private readonly ITaskDelayProvider taskDelayProvider;
        private readonly IDateProvider dateProvider;

        public CovidTrackingHostedService(IOptions<CovidTrackingSettings> covidTrackingSettings, 
            IHostApplicationLifetime applicationLifetime, ILogger<CovidTrackingHostedService> log, 
            ICovidDownloadingService covidDownloadingService, ITaskDelayProvider taskDelayProvider,
            IDateProvider dateProvider)
        {
            this.covidTrackingSettings = covidTrackingSettings;
            this.applicationLifetime = applicationLifetime;
            this.log = log;
            this.covidDownloadingService = covidDownloadingService;
            this.taskDelayProvider = taskDelayProvider;
            this.dateProvider = dateProvider;
        }

        private DateTimeOffset FetchDate { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            FetchDate = dateProvider.DateTimeOffsetUtcNow().ConvertUtcToPolishTime().Midnight()
                .AddHours(covidTrackingSettings.Value.FetchDataHour)
                .ToUniversalTime();

            await WorkerAsync(stoppingToken);
        }

        private async Task WorkerAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (dateProvider.DateTimeOffsetUtcNow() >= FetchDate)
                {
                    try
                    {
                        var result = await covidDownloadingService.DownloadCovidByJsonAsync();
                        if (result)
                        {
                            FetchDate = FetchDate.AddDays(1);
                            log.LogInformation(
                                $"[{nameof(CovidTrackingHostedService)}] - Data successfully downloaded" +
                                " or is up to date");
                        }
                        else
                        {
                            log.LogError(
                                @$"[{nameof(CovidTrackingHostedService)}] - Problem with downloading data");

                            if (dateProvider.DateTimeOffsetUtcNow() >= FetchDate.AddHours(3))
                                await taskDelayProvider.Delay(TimeSpan.FromHours(3), stoppingToken);
                            else
                                await taskDelayProvider.Delay(TimeSpan.FromMinutes(20), stoppingToken);
                        }
                    }
                    catch (CovidParseException ex)
                    {
                        log.LogError(ex, $"[{nameof(CovidTrackingHostedService)}] - CovidParseException");
                        applicationLifetime.StopApplication();
                    }
                }
                await taskDelayProvider.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
