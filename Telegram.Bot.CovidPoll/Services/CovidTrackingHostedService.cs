using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll.Services
{
    public class CovidTrackingHostedService : BackgroundService
    {
        private readonly IOptions<CovidTrackingSettings> covidTrackingSettings;
        private readonly ICovidRepository covidRepository;
        private readonly IHttpClientFactory httpClient;
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly IBotPollResultSenderService botPollResultSender;
        private readonly ILogger<CovidTrackingHostedService> log;
        private readonly ICovidDownloadingService covidDownloadingService;

        public CovidTrackingHostedService(IOptions<CovidTrackingSettings> covidTrackingSettings,
                                          ICovidRepository covidRepository,
                                          IHttpClientFactory httpClient,
                                          IHostApplicationLifetime applicationLifetime,
                                          IBotPollResultSenderService botPollResultSender,
                                          ILogger<CovidTrackingHostedService> log,
                                          ICovidDownloadingService covidDownloadingService)
        {
            this.covidTrackingSettings = covidTrackingSettings;
            this.covidRepository = covidRepository;
            this.httpClient = httpClient;
            this.applicationLifetime = applicationLifetime;
            this.botPollResultSender = botPollResultSender;
            this.log = log;
            this.covidDownloadingService = covidDownloadingService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var fetchDate = DateTime.UtcNow.Date.AddHours(covidTrackingSettings.Value.FetchDataHourUtc);
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
                                .AddHours(covidTrackingSettings.Value.FetchDataHourUtc);
                            log.LogInformation(
                                @$"[{nameof(CovidTrackingHostedService)}]: 
                                    Data successfully downloaded or is up to date");
                        }
                        else
                        {
                            log.LogError(
                                @$"[{nameof(CovidTrackingHostedService)}]: Problem with downloading data");
                            await Task.Delay(TimeSpan.FromMinutes(20), stoppingToken);
                        }
                    }
                    catch (CovidParseException ex)
                    {
                        log.LogError(ex, $"[{nameof(CovidTrackingHostedService)}]: CovidParseException");
                        applicationLifetime.StopApplication();
                    }
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
