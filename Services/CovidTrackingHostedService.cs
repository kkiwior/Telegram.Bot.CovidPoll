using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Repositories;

namespace Telegram.Bot.CovidPoll.Services
{
    public class CovidTrackingHostedService : BackgroundService
    {
        private readonly ILogger<CovidTrackingHostedService> logger;
        private readonly HttpClient httpClient;
        private readonly IOptions<CovidTrackingSettings> covidTrackingSettings;
        private readonly ICovidRepository covidRepository;
        public CovidTrackingHostedService(ILogger<CovidTrackingHostedService> logger,
                                    IOptions<CovidTrackingSettings> covidTrackingSettings,
                                    ICovidRepository covidRepository)
        {
            this.logger = logger;
            this.httpClient = new HttpClient();
            this.covidTrackingSettings = covidTrackingSettings;
            this.covidRepository = covidRepository;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Odpalanko");
            await BackgroundProcessing(stoppingToken);
        }
        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            var fetchDate = DateTime.UtcNow.Date.AddHours(covidTrackingSettings.Value.FetchDataHourUtc);
            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= fetchDate)
                {
                    if (await SaveTotalCasesAsync())
                        fetchDate = DateTime.UtcNow.Date.AddDays(1).AddHours(covidTrackingSettings.Value.FetchDataHourUtc);
                    else
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                await Task.Delay(1000);
            }
        }
        private async Task<bool> SaveTotalCasesAsync()
        {
            var latestCovid = await covidRepository.FindLatestAsync();
            if (latestCovid != null && DateTime.UtcNow.Date <= latestCovid.Date)
                return true;

            var response = await httpClient.GetAsync(covidTrackingSettings.Value.Url);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Sprawdzono");
                var htmlContent = await response.Content.ReadAsStringAsync();

                var pattern = "<pre id=\"registerData\" class=\"hide\">([^<]*)<\\/pre>";
                var datePattern = "^{\"description\":\"(\\D*)(\\d*.\\d*.\\d* \\d*:\\d*)\"";
                var casesPattern = @"Cała Polska;([0-9 ]*);";
                var regexContent = new Regex(pattern).Match(htmlContent).Groups[1].Value;
                if (DateTime.TryParse(new Regex(datePattern).Match(regexContent).Groups[2].Value, out var updateDate))
                    if (DateTime.UtcNow.Date > updateDate.ToUniversalTime().Date)
                        return false;

                if (int.TryParse(new Regex(casesPattern).Match(regexContent).Groups[1].Value.Replace(" ", ""), out var totalCases))
                {
                    await covidRepository.AddAsync(new Db.Covid
                    {
                        TotalCases = totalCases,
                        DownloadedSuccessfully = true,
                        Date = DateTime.UtcNow.Date
                    });
                    return true;
                }
            }
            return false;
        }
    }
}
