using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Repositories;

namespace Telegram.Bot.CovidPoll.Services
{
    public class CovidTrackingHostedService : BackgroundService
    {
        private readonly IOptions<CovidTrackingSettings> covidTrackingSettings;
        private readonly ICovidRepository covidRepository;
        private readonly IHttpClientFactory httpClient;
        private readonly IHostApplicationLifetime applicationLifetime;
        private static bool firstExecute = false;
        public CovidTrackingHostedService(IOptions<CovidTrackingSettings> covidTrackingSettings,
                                          ICovidRepository covidRepository,
                                          IHttpClientFactory httpClient,
                                          IHostApplicationLifetime applicationLifetime)
        {
            this.covidTrackingSettings = covidTrackingSettings;
            this.covidRepository = covidRepository;
            this.httpClient = httpClient;
            this.applicationLifetime = applicationLifetime;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }
        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            var fetchDate = DateTime.UtcNow.Date.AddHours(covidTrackingSettings.Value.FetchDataHourUtc);
            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= fetchDate || !firstExecute)
                {
                    firstExecute = true;
                    try
                    {
                        if (await SaveTotalCasesAsync())
                        {
                            fetchDate = DateTime.UtcNow.Date.AddDays(1)
                                .AddHours(covidTrackingSettings.Value.FetchDataHourUtc);
                            Log.Information($"Data successfully downloaded or is up to date");
                        }
                        else
                        {
                            Log.Error("Problem with downloading data");
                            await Task.Delay(TimeSpan.FromHours(3), stoppingToken);
                        }
                    }
                    catch (CovidParseException ex)
                    {
                        Log.Error($"CovidParseException: {ex.Message}");
                        applicationLifetime.StopApplication();
                    }
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
        private async Task<bool> SaveTotalCasesAsync()
        {
            var httpClient = this.httpClient.CreateClient();
            var latestCovid = await covidRepository.FindLatestAsync();
            if (latestCovid != null && DateTime.UtcNow.Date <= latestCovid.Date)
                return true;

            var response = await httpClient.GetAsync(covidTrackingSettings.Value.Url);
            if (response.IsSuccessStatusCode)
            {
                var htmlContent = await response.Content.ReadAsStringAsync();

                var pattern = "<pre id=\"registerData\" class=\"hide\">([^<]*)<\\/pre>";
                var datePattern = "^{\"description\":\"(\\D*)(\\d*.\\d*.\\d* \\d*:\\d*)\"";
                var casesPattern = @"Cała Polska;([0-9 ]*);";
                var regexContent = new Regex(pattern).Match(htmlContent).Groups[1].Value;
                if (DateTime.TryParse(new Regex(datePattern).Match(regexContent).Groups[2].Value, out var updateDate))
                {
                    if (latestCovid != null && latestCovid.Date >= updateDate.ToUniversalTime().Date)
                        return false;
                }
                else
                {
                    throw new CovidParseException($"DateTime parse exception, regexContent = {regexContent}");
                }

                if (int.TryParse(new Regex(casesPattern).Match(regexContent).Groups[1].Value.Replace(" ", ""), out var totalCases))
                {
                    await covidRepository.AddAsync(new Db.Covid
                    {
                        TotalCases = totalCases,
                        Date = DateTime.UtcNow.Date
                    });
                    return true;
                }
                else
                {
                    throw new CovidParseException($"Cases parse exception, regexContent = {regexContent}");
                }
            }
            return false;
        }
    }
}
