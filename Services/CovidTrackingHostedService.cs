using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
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
        private readonly BotPollResultSenderService botPollResultSender;
        private static bool firstExecute = false;
        public CovidTrackingHostedService(IOptions<CovidTrackingSettings> covidTrackingSettings,
                                          ICovidRepository covidRepository,
                                          IHttpClientFactory httpClient,
                                          IHostApplicationLifetime applicationLifetime,
                                          BotPollResultSenderService botPollResultSender)
        {
            this.covidTrackingSettings = covidTrackingSettings;
            this.covidRepository = covidRepository;
            this.httpClient = httpClient;
            this.applicationLifetime = applicationLifetime;
            this.botPollResultSender = botPollResultSender;
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
                if (DateTime.UtcNow >= fetchDate)
                {
                    try
                    {
                        var result = await SaveTotalCasesAsync();
                        if (result)
                        {
                            fetchDate = DateTime.UtcNow.Date.AddDays(1)
                                .AddHours(covidTrackingSettings.Value.FetchDataHourUtc);
                            Log.Information($"[{nameof(CovidTrackingHostedService)}]: Data successfully downloaded or is up to date");
                        }
                        else
                        {
                            Log.Error($"[{nameof(CovidTrackingHostedService)}]: Problem with downloading data");
                            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                        }
                    }
                    catch (CovidParseException ex)
                    {
                        Log.Error(ex, $"[{nameof(CovidTrackingHostedService)}]: CovidParseException");
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

                var datePattern =
                    "<div id=\"global-stats\">\\n<div class=\"global-stats\">\\n<p>Dane pochodzą z Ministerstwa Zdrowia,\\s*aktualne\\s*na\\s*:\\s*(\\d{2}.\\d{2}.\\d{4} \\d{2}:\\d{2})\\s*<a";
                var casesPattern = "<pre id=\"registerData\" class=\"hide\">{\"description\":\".*\",\"data\":\".*Cały kraj;([0-9]*).*<\\/pre>";
                var dateRegex = new Regex(datePattern, RegexOptions.IgnoreCase).Match(htmlContent);
                var casesRegex = new Regex(casesPattern, RegexOptions.IgnoreCase).Match(htmlContent);

                if (!dateRegex.Success || dateRegex.Groups.Count < 2)
                    throw new CovidParseException($"dataRegex failed");

                if (DateTime.TryParse(dateRegex.Groups[1].Value, CultureInfo.GetCultureInfo("pl-PL"), DateTimeStyles.None, out var updateDate))
                {
                    if (latestCovid != null && latestCovid.Date >= updateDate.ToUniversalTime().Date)
                        return false;
                }
                else
                {
                    throw new CovidParseException($"DateTime parse exception, regexContent = {dateRegex.Groups[1].Value}");
                }

                if (!casesRegex.Success || casesRegex.Groups.Count < 2)
                    throw new CovidParseException($"casesRegex failed");

                if (int.TryParse(casesRegex.Groups[1].Value.Replace(" ", ""), out var newCases))
                {
                    await covidRepository.AddAsync(new Db.Covid
                    {
                        NewCases = newCases,
                        Date = DateTime.UtcNow.Date
                    });
                    botPollResultSender.SendPredictionsResultsToChats();
                    return true;
                }
                else
                {
                    throw new CovidParseException($"Cases parse exception, regexContent = {casesRegex.Groups[1].Value.Replace(" ", "")}");
                }
            }
            return false;
        }
    }
}
